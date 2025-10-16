namespace AcsConversationGateway.Api.Endpoints;

public static class SupportTicketEndpoints
{
    public static void MapSupportTicketEndpoints(this WebApplication app, MessageProcessingService manager)
    {
        var conversationGroup = app.MapGroup("/tickets")
            .WithOpenApi()
            .WithTags("Support Ticket Operations");

        conversationGroup.MapGet("/", GetSupportTickets)
            .WithName("GetSupportTickets")
            .WithSummary("Get support tickets for a customer or all customers");

        async Task<IResult> GetSupportTickets(int? customerId, CancellationToken cancellationToken)
        {
            var history = await manager.GetSupportTicketsAsync(customerId);
            return Results.Ok(history);
        }
    }
}