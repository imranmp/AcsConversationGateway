namespace AcsConversationGateway.Api.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this WebApplication app, MessageProcessingService manager)
    {
        var customerGroup = app.MapGroup("/customers")
            .WithOpenApi()
            .WithTags("Customer Operations");

        customerGroup.MapGet("/", GetCustomers)
            .WithName("GetCustomers")
            .WithSummary("Get all customers");

        async Task<IResult> GetCustomers(CancellationToken cancellationToken)
        {
            var customers = await manager.GetCustomersAsync();
            return Results.Ok(customers);
        }
    }
}