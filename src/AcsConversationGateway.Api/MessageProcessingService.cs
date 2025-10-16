using AcsConversationGateway.Api.Contracts;
using AcsConversationGateway.Api.Models;
using AcsConversationGateway.Api.Models.Enums;
using AcsConversationGateway.Api.Services;
using Azure;
using Azure.Communication.Email;
using Azure.Communication.Sms;

namespace AcsConversationGateway.Api;

public class MessageProcessingService
{
    private readonly IDataService _dataService;
    private readonly IConfiguration _configuration;

    private readonly SmsClient _smsClient;
    private readonly EmailClient _emailClient;

    private readonly string _acsPhoneNumber;
    private readonly string _acsEmail;
    private readonly string _replyToEmail;

    private readonly bool _mockEmail;
    private readonly bool _mockSms;

    public MessageProcessingService(IConfiguration config, IDataService dataService)
    {
        _dataService = dataService;
        _configuration = config;

        var acsConn = config["AzureCommunicationService:ConnectionString"]!;
        _acsPhoneNumber = config["AzureCommunicationService:PhoneNumber"]!;
        _acsEmail = config["AzureCommunicationService:FromEmail"]!;
        _replyToEmail = config["AzureCommunicationService:ReplyToEmail"]!;

        // Read feature flags
        _mockEmail = config.GetValue<bool>("FeatureFlags:MockEmail");
        _mockSms = config.GetValue<bool>("FeatureFlags:MockSms");

        _smsClient = new SmsClient(acsConn);
        _emailClient = new EmailClient(acsConn);
    }

    public async Task SendSmsAsync(int customerId, string message, CancellationToken cancellationToken = default)
    {
        Customer? customer = await _dataService.GetCustomerByIdAsync(customerId)
            ?? throw new Exception($"Customer not found for Id {customerId}");

        SupportTicket? ticket = await GetOpenSupportTicketAsync(customerId)
            ?? throw new Exception("No open support ticket found for customer");

        string? messageId;

        if (_mockSms)
        {
            // Mock SMS - generate a fake message ID
            messageId = $"mock-sms-{Guid.NewGuid():N}";
        }
        else
        {
            // Send actual SMS via ACS
            Response<SmsSendResult> result = await _smsClient.SendAsync(from: _acsPhoneNumber, to: customer.Phone, message: message, cancellationToken: cancellationToken);
            messageId = result.Value.MessageId;
        }

        ticket.Messages
            .Add(new Message
            {
                Channel = ChannelType.SMS,
                From = _acsPhoneNumber,
                To = customer.Phone,
                Content = message,
                MessageId = messageId
            });

        await _dataService.UpdateSupportTicketAsync(ticket);
    }

    public async Task ProcessInboundSmsAsync(string jsonContent)
    {
        // Simplified parsing — ACS posts SmsReceivedEvent
        // Example JSON: {"data":{"from":"+15551234567","to":"+15559876543","message":"Hi there!"}}
        var evt = System.Text.Json.JsonDocument.Parse(jsonContent).RootElement;
        var data = evt.GetProperty("data");

        var from = data.GetProperty("from").GetString()!;
        var to = data.GetProperty("to").GetString()!;
        var content = data.GetProperty("message").GetString()!;
        var messageId = data.GetProperty("messageId").GetString()!;

        Customer? customer = await _dataService.GetCustomerByPhoneAsync(from)
            ?? throw new Exception($"Customer not found for number {from}");

        // Find an open ticket for this customer
        var tickets = await _dataService.GetSupportTicketsAsync();
        SupportTicket? ticket = tickets.FirstOrDefault(t => t.CustomerId == customer.Id && t.Status == TicketStatus.Open)
            ?? throw new Exception("Support ticket not found for customer");

        ticket.Messages
            .Add(new Message
            {
                Channel = ChannelType.SMS,
                From = from,
                To = to,
                Content = content,
                MessageId = messageId,
                Timestamp = DateTimeOffset.UtcNow
            });

        await _dataService.UpdateSupportTicketAsync(ticket);
    }

    public async Task SendEmailAsync(int customerId, string subject, string body, CancellationToken cancellationToken = default)
    {
        Customer? customer = await _dataService.GetCustomerByIdAsync(customerId)
            ?? throw new Exception($"Customer not found for Id {customerId}");

        SupportTicket? ticket = await GetOpenSupportTicketAsync(customerId)
            ?? throw new Exception("No open support ticket found for customer");

        string? conversationId, messageId;
        messageId = Guid.NewGuid().ToString();

        if (_mockEmail)
        {
            // Mock Email - generate a fake operation/conversation ID
            conversationId = $"mock-email-{Guid.NewGuid():N}";
        }
        else
        {
            // Send actual email via ACS
            var email = new EmailMessage(_acsEmail, customer.Email, new EmailContent($"{subject} - {DateTime.UtcNow.ToShortTimeString()}")
            {
                Html = $"""
                 <html>
                    <body>
                        <p>{body}</p>
                    </body>
                 </html>
                 """
            });

            // Format replyTo address to help identify customer replies by adding +ticket.Id to _replyToEmail username
            var atIdx = _replyToEmail.IndexOf('@');
            string replyTo = atIdx > 0
                ? $"{_replyToEmail[..atIdx]}+{ticket.Id}{_replyToEmail[atIdx..]}"
                : _replyToEmail;

            email.ReplyTo.Add(new EmailAddress(replyTo, "Sales Associate A"));

            EmailSendOperation sendOperation = await _emailClient.SendAsync(WaitUntil.Completed, email, cancellationToken);
            conversationId = sendOperation.Id;
        }

        ticket.Messages
            .Add(new Message
            {
                Channel = ChannelType.Email,
                From = _acsEmail,
                To = customer.Email,
                Content = body,
                ConversationId = conversationId,
                MessageId = messageId
            });

        await _dataService.UpdateSupportTicketAsync(ticket);
    }

    public async Task ProcessInboundEmailAsync(EmailData emailData)
    {
        int ticketId = emailData.To.Contains('+') && emailData.To.Contains('@')
            ? int.Parse(emailData.To.Split('+', '@')[1])
            : -1;

        if (ticketId == -1)
        {
            throw new Exception("Ticket ID not found in the email address.");
        }

        SupportTicket? ticket = await _dataService.GetSupportTicketByIdAsync(ticketId)
            ?? throw new Exception($"Ticket not found for ticket id {ticketId}.");

        if (ticket.Messages.Any(m => m.MessageId == emailData.Id))
        {
            // Duplicate email, already processed
            return;
        }

        ticket.Messages
            .Add(new Message
            {
                Channel = ChannelType.Email,
                From = emailData.From,
                To = emailData.To,
                Content = emailData.BodyPreview,
                ConversationId = emailData.ConversationId,
                MessageId = emailData.Id,
                Timestamp = emailData.ReceivedDateTime
            });

        await _dataService.UpdateSupportTicketAsync(ticket);
    }

    public async Task<IEnumerable<Customer>> GetCustomersAsync() => await _dataService.GetCustomersAsync();

    public async Task<IEnumerable<SupportTicket>> GetSupportTicketsAsync(int? customerId = null)
    {
        var tickets = await _dataService.GetSupportTicketsAsync();

        if (customerId is null)
            return tickets;
        else
            return tickets.Where(x => x.CustomerId == customerId);
    }

    private async Task<SupportTicket?> GetOpenSupportTicketAsync(int customerId)
    {
        var tickets = await _dataService.GetSupportTicketsAsync();
        return tickets.FirstOrDefault(t => t.CustomerId == customerId && t.Status == TicketStatus.Open);
    }
}