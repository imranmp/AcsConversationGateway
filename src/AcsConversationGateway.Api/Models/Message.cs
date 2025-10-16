using AcsConversationGateway.Api.Models.Enums;
using System.Text.Json.Serialization;

namespace AcsConversationGateway.Api.Models;

public class Message
{
    public required ChannelType Channel { get; set; }
    public required string From { get; set; }
    public required string To { get; set; }
    public required string Content { get; set; }
    [JsonIgnore]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    [JsonIgnore]
    public string? ConversationId { get; set; }
    [JsonIgnore]
    public string? MessageId { get; set; }
}
