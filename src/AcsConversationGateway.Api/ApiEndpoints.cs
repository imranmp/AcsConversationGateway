using AcsConversationGateway.Api.Endpoints;

namespace AcsConversationGateway.Api;

public static class ApiEndpoints
{
    /// <summary>
    /// Maps all conversation API endpoints to the application
    /// </summary>
    /// <param name="app">The web application instance</param>
    public static void MapApiEndpoints(this WebApplication app)
    {
        var manager = app.Services.GetRequiredService<MessageProcessingService>();

        // Map endpoint groups
        app.MapSmsEndpoints(manager);
        app.MapEmailEndpoints(manager);
        app.MapSupportTicketEndpoints(manager);
        app.MapCustomerEndpoints(manager);
    }
}