namespace AcsConversationGateway.Api.Contracts;

// Request DTOs
public record SmsRequest(int CustomerId, string Message);

public record EmailRequest(int CustomerId, string Subject, string Body);

public record EmailData(string From, string To, string BodyPreview, DateTimeOffset ReceivedDateTime, string ConversationId, string? Id);