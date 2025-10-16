namespace AcsConversationGateway.Function.Models;

public record EmailData(string From, string To, string BodyPreview, DateTimeOffset ReceivedDateTime, string ConversationId, string? Id);
