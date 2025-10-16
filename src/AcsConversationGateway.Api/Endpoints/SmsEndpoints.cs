using AcsConversationGateway.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace AcsConversationGateway.Api.Endpoints;

public static class SmsEndpoints
{
    public static void MapSmsEndpoints(this WebApplication app, MessageProcessingService manager)
    {
        var smsGroup = app.MapGroup("/sms")
            .WithOpenApi()
            .WithTags("SMS Operations");

        smsGroup.MapPost("/send", SendSmsAsync)
            .WithName("SendSms")
            .WithSummary("Send SMS to customer");

        smsGroup.MapPost("/inbound", ProcessInboundSmsAsync)
            .WithName("ProcessInboundSms")
            .WithSummary("Process incoming SMS from ACS webhook");

        async Task<IResult> SendSmsAsync([FromBody] SmsRequest request, CancellationToken cancellationToken)
        {
            await manager.SendSmsAsync(request.CustomerId, request.Message, cancellationToken);
            return Results.Ok("SMS sent.");
        }

        async Task<IResult> ProcessInboundSmsAsync(HttpRequest httpRequest, CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(httpRequest.Body);
            var json = await reader.ReadToEndAsync(cancellationToken);

            await manager.ProcessInboundSmsAsync(json);

            return Results.Ok();
        }
    }
}