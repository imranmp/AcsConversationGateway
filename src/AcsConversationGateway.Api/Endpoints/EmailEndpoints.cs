using AcsConversationGateway.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace AcsConversationGateway.Api.Endpoints;

public static class EmailEndpoints
{
    public static void MapEmailEndpoints(this WebApplication app, MessageProcessingService manager)
    {
        var emailGroup = app.MapGroup("/email")
            .WithOpenApi()
            .WithTags("Email Operations");

        emailGroup.MapPost("/send", SendEmailAsync)
            .WithName("SendEmail")
            .WithSummary("Send email to customer");

        emailGroup.MapPost("/inbound", ProcessInboundEmailAsync)
            .WithName("ProcessInboundEmail")
            .WithSummary("Process incoming email from external system");

        async Task<IResult> SendEmailAsync([FromBody] EmailRequest request, CancellationToken cancellationToken)
        {
            await manager.SendEmailAsync(request.CustomerId, request.Subject, request.Body, cancellationToken);
            return Results.Ok("Email sent.");
        }

        async Task<IResult> ProcessInboundEmailAsync(EmailData emailData, CancellationToken cancellationToken)
        {
            await manager.ProcessInboundEmailAsync(emailData);
            return Results.Ok();
        }
    }
}