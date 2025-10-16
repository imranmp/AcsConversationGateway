using AcsConversationGateway.Function.Helpers;
using AcsConversationGateway.Function.Models;

namespace AcsConversationGateway.Function.Functions;

public class EmailProcessingHandler(ILogger<EmailProcessingHandler> logger, IConfiguration configuration)
{
    private readonly ILogger<EmailProcessingHandler> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    [Function("EmailProcessingHandler")]
    public async Task Run([ServiceBusTrigger("%EmailProcessingQueueName%", Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage message)
    {
        try
        {
            var messageBody = message.Body.ToString();
            _logger.LogInformation("Processing email message: {MessageBody}", messageBody);

            var emailProcessingMessage = JsonSerializer.Deserialize<EmailProcessingMessage>(messageBody);
            if (emailProcessingMessage == null)
            {
                _logger.LogWarning("Invalid message format received");
                return;
            }

            var messageId = emailProcessingMessage.MessageId;
            var userId = emailProcessingMessage.UserId;

            _logger.LogInformation("Processing message with ID: {MessageId} for user: {UserId}", messageId, userId);

            var graphClient = GraphClientHelper.CreateGraphClient(_configuration);

            Message? graphMessage = await graphClient.Users[userId]
                .Messages[messageId]
                .GetAsync(x =>
                {
                    x.QueryParameters.Select = ["body", "uniqueBody", "from", "torecipients", "bodypreview", "conversationid", "receiveddatetime"];
                });

            if (graphMessage == null)
            {
                _logger.LogWarning("Message with ID {MessageId} not found.", messageId);
                return;
            }

            var fullBodyHtml = graphMessage.Body?.Content;
            var uniqueBodyHtml = graphMessage.UniqueBody?.Content;
            var plainText = "No body content";

            if (!string.IsNullOrEmpty(uniqueBodyHtml) && graphMessage.UniqueBody?.ContentType == BodyType.Html)
            {
                plainText = EmailBodyHelper.ConvertHtmlToPlainText(uniqueBodyHtml);
            }
            else if (!string.IsNullOrEmpty(fullBodyHtml) && graphMessage.Body?.ContentType == BodyType.Html)
            {
                plainText = EmailBodyHelper.ConvertHtmlToPlainText(fullBodyHtml);
            }
            else
            {
                plainText = graphMessage.Body?.Content ?? plainText;
            }

            var emailData = new EmailData(
                    From: graphMessage.From?.EmailAddress?.Address ?? "From Address Missing",
                    To: graphMessage.ToRecipients?.FirstOrDefault()?.EmailAddress?.Address ?? "To Address Missing",
                    plainText,
                    graphMessage.ReceivedDateTime ?? DateTime.UtcNow,
                    graphMessage.ConversationId ?? "Conversation Id Missing",
                    graphMessage.Id
                );

            var apiUrl = _configuration["InternalApiUrl"];
            var httpClient = new HttpClient() { BaseAddress = new Uri(apiUrl!) };
            var content = new StringContent(JsonSerializer.Serialize(emailData), Encoding.UTF8, "application/json");
            await httpClient.PostAsync("/email/inbound", content);

            _logger.LogInformation("Successfully processed message {MessageId} and sent to API", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email message");
            throw; // Rethrow to trigger Service Bus retry logic
        }
    }
}