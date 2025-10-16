namespace AcsConversationGateway.Function.Models;

public record EmailNotificationPayload(Value[]? Value);

public record Value(string? SubscriptionId,
                    DateTime SubscriptionExpirationDateTime,
                    string? ChangeType,
                    string? Resource,
                    Resourcedata? ResourceData,
                    string? ClientState,
                    string? TenantId);

public record Resourcedata(string? Odatatype,
                           string? Odataid,
                           string? Odataetag,
                           string? Id);
