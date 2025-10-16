using AcsConversationGateway.Function.Helpers;

namespace AcsConversationGateway.Function.Functions;

public class RenewGraphSubscription(ILoggerFactory loggerFactory, IConfiguration configuration)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<RenewGraphSubscription>();
    private readonly IConfiguration _configuration = configuration;

    [Function("RenewGraphSubscription")]
    // Uncomment the TimerTrigger attribute and comment the HttpTrigger attribute to enable timer-based execution to run every ~48hrs
    //public async Task Run([TimerTrigger("0 0 2 1-31/2 * *")] TimerInfo myTimer)
    public async Task Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        var notificationUrl = _configuration["NotificationUrl"];
        var userId = _configuration["TargetUserId"];
        var secretClientState = _configuration["ClientState"];
        var resource = $"users/{userId}/mailFolders('Inbox')/messages";

        var graphClient = GraphClientHelper.CreateGraphClient(_configuration);

        // Delete existing subscriptions
        var subscriptions = await graphClient.Subscriptions.GetAsync();
        var subscriptionList = subscriptions?.Value ?? [];
        foreach (var sub in subscriptionList.Where(s =>
            string.Equals(s.Resource, resource, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.NotificationUrl, notificationUrl, StringComparison.OrdinalIgnoreCase)))
        {
            if (!string.IsNullOrEmpty(sub.Id))
            {
                await graphClient.Subscriptions[sub.Id].DeleteAsync();
                _logger.LogInformation("Deleted subscription for {Resource} resource", resource);
            }
        }

        // Create new subscription
        var newSub = new Subscription
        {
            ChangeType = "created",
            NotificationUrl = notificationUrl,
            Resource = resource,
            ExpirationDateTime = DateTime.UtcNow.AddMinutes(4230),
            ClientState = secretClientState
        };

        var createdSub = await graphClient.Subscriptions.PostAsync(newSub);
        if (createdSub == null)
        {
            _logger.LogError("Failed to create new subscription.");
            return;
        }
        _logger.LogInformation("Created new subscription: {@Subscription}", createdSub);
    }
}