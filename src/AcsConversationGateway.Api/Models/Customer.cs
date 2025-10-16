namespace AcsConversationGateway.Api.Models;

public class Customer
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
}
