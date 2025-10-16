using AcsConversationGateway.Api.Models;

namespace AcsConversationGateway.Api.Services;

public interface IDataService
{
    Task<List<Customer>> GetCustomersAsync();

    Task<Customer?> GetCustomerByIdAsync(int customerId);

    Task<Customer?> GetCustomerByPhoneAsync(string phone);

    Task<List<SupportTicket>> GetSupportTicketsAsync();

    Task<SupportTicket?> GetSupportTicketByIdAsync(int ticketId);

    Task SaveSupportTicketsAsync(List<SupportTicket> supportTickets);

    Task UpdateSupportTicketAsync(SupportTicket ticket);
}