using AcsConversationGateway.Api.Models.Enums;
using System.Text.Json.Serialization;

namespace AcsConversationGateway.Api.Models;

public class SupportTicket
{
    //use UUID v7 for Id as primary key
    public required int Id { get; set; }
    public required int CustomerId { get; set; }
    public required int EmployeeId { get; set; }
    public required string Subject { get; set; }

    [JsonIgnore]
    public string? Description { get; set; }
    [JsonIgnore]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    [JsonIgnore]
    public DateTimeOffset? ClosedAt { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public List<Message> Messages { get; set; } = [];
}
