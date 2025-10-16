using AcsConversationGateway.Function.Models;

namespace AcsConversationGateway.Function.Functions;

public class EmailNotificationHandler(ILogger<EmailNotificationHandler> logger, IConfiguration configuration)
{
    private readonly ILogger<EmailNotificationHandler> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    [Function("EmailNotificationHandler")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        // Handle validation token for subscription validation
        req.Query.TryGetValue("validationToken", out var validationToken);
        if (!string.IsNullOrEmpty(validationToken))
        {
            _logger.LogInformation("Validation token received: {ValidationToken}", validationToken!);
            return new ContentResult
            {
                Content = validationToken,
                ContentType = "text/plain",
                StatusCode = (int)HttpStatusCode.Created
            };
        }

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        _logger.LogInformation("Received notification: {RequestBody}", requestBody);

        var notification = JsonSerializer.Deserialize<EmailNotificationPayload>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (notification?.Value?[0] == null)
        {
            _logger.LogWarning("No notification value found in the request.");
            return new OkResult();
        }

        var secretClientState = _configuration["ClientState"];
        var userId = _configuration["TargetUserId"];

        if (notification?.Value?[0]?.ClientState != secretClientState)
        {
            _logger.LogWarning("Client state mismatch. Expected: {Expected}, Received: {Received}", secretClientState, notification?.Value?[0]?.ClientState);
            return new OkResult();
        }

        string? messageId = notification?.Value?[0]?.ResourceData?.Id;
        if (string.IsNullOrEmpty(messageId))
        {
            _logger.LogWarning("Message ID not found in the notification.");
            return new OkResult();
        }

        _logger.LogInformation("Processing message with ID: {MessageId}", messageId);

        // Send messageId to Service Bus queue for processing
        var serviceBusConnectionString = _configuration["ServiceBusConnectionString"];
        var queueName = _configuration["EmailProcessingQueueName"] ?? "email-processing";

        try
        {
            await using var client = new ServiceBusClient(serviceBusConnectionString);
            var sender = client.CreateSender(queueName);

            var messagePayload = new EmailProcessingMessage(messageId, userId!);
            var serviceBusMessage = new ServiceBusMessage(JsonSerializer.Serialize(messagePayload))
            {
                MessageId = Guid.NewGuid().ToString(),
                ContentType = "application/json"
            };

            await sender.SendMessageAsync(serviceBusMessage);
            _logger.LogInformation("Message {MessageId} queued for processing", messageId);

            return new CreatedResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue message {MessageId} for processing", messageId);
            return new StatusCodeResult(500);
        }
    }
}