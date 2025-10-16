using AcsConversationGateway.Api.Models;
using AcsConversationGateway.Api.Models.Enums;
using System.Text.Json;

namespace AcsConversationGateway.Api.Services;

public class JsonDataService : IDataService
{
    private readonly string _dataDirectory;
    private readonly string _customersFilePath;
    private readonly string _supportTicketsFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    private List<Customer>? _customers;
    private List<SupportTicket>? _supportTickets;

    public JsonDataService()
    {
        // Data directory and file paths for JSON storage for demo purposes
        _dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        _customersFilePath = Path.Combine(_dataDirectory, "customers.json");
        _supportTicketsFilePath = Path.Combine(_dataDirectory, "support-tickets.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        EnsureDataDirectoryExists();
        InitializeDefaultDataIfNeeded();
    }

    public async Task<List<Customer>> GetCustomersAsync()
    {
        _customers ??= await LoadCustomersFromFileAsync();
        return _customers;
    }

    public async Task<List<SupportTicket>> GetSupportTicketsAsync()
    {
        _supportTickets ??= await LoadSupportTicketsFromFileAsync();
        return _supportTickets;
    }

    public async Task SaveSupportTicketsAsync(List<SupportTicket> supportTickets)
    {
        _supportTickets = supportTickets;
        var json = JsonSerializer.Serialize(supportTickets, _jsonOptions);
        await File.WriteAllTextAsync(_supportTicketsFilePath, json);
    }

    public async Task<Customer?> GetCustomerByIdAsync(int customerId)
    {
        var customers = await GetCustomersAsync();
        return customers.FirstOrDefault(c => c.Id == customerId);
    }

    public async Task<Customer?> GetCustomerByPhoneAsync(string phone)
    {
        var customers = await GetCustomersAsync();
        return customers.FirstOrDefault(c => c.Phone == phone);
    }

    public async Task<SupportTicket?> GetSupportTicketByIdAsync(int ticketId)
    {
        var tickets = await GetSupportTicketsAsync();
        return tickets.FirstOrDefault(t => t.Id == ticketId);
    }

    public async Task UpdateSupportTicketAsync(SupportTicket ticket)
    {
        var tickets = await GetSupportTicketsAsync();
        var existingTicket = tickets.FirstOrDefault(t => t.Id == ticket.Id);

        if (existingTicket != null)
        {
            // Update existing ticket
            var index = tickets.IndexOf(existingTicket);
            tickets[index] = ticket;

            await SaveSupportTicketsAsync(tickets);
        }
    }

    private async Task<List<Customer>> LoadCustomersFromFileAsync()
    {
        if (!File.Exists(_customersFilePath))
        {
            return [];
        }

        try
        {
            var json = await File.ReadAllTextAsync(_customersFilePath);
            return JsonSerializer.Deserialize<List<Customer>>(json, _jsonOptions) ?? [];
        }
        catch (JsonException)
        {
            // If file is corrupted, return empty list and log error in real apps
            return [];
        }
    }

    private async Task<List<SupportTicket>> LoadSupportTicketsFromFileAsync()
    {
        if (!File.Exists(_supportTicketsFilePath))
        {
            return [];
        }

        try
        {
            var json = await File.ReadAllTextAsync(_supportTicketsFilePath);
            return JsonSerializer.Deserialize<List<SupportTicket>>(json, _jsonOptions) ?? [];
        }
        catch (JsonException)
        {
            // If file is corrupted, return empty list and log error in real apps
            return [];
        }
    }

    private void EnsureDataDirectoryExists()
    {
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }
    }

    private void InitializeDefaultDataIfNeeded()
    {
        // Initialize with default customers if file doesn't exist
        if (!File.Exists(_customersFilePath))
        {
            var defaultCustomers = new List<Customer>
            {
                new() { Id = 101, Email = "imran+1@example.com", Phone = "+14075551111" },
                new() { Id = 102, Email = "imran+2@example.com", Phone = "+14075552222" }
            };

            var json = JsonSerializer.Serialize(defaultCustomers, _jsonOptions);
            File.WriteAllText(_customersFilePath, json);
        }

        // Initialize with empty support tickets if file doesn't exist
        if (!File.Exists(_supportTicketsFilePath))
        {
            var defaultTickets = new List<SupportTicket>
            {
                new()
                {
                    Id = 9901,
                    CustomerId = 101,
                    EmployeeId = 201,
                    Subject = "Support Ticket for Customer 101",
                    Status = TicketStatus.Open,
                    Messages = []
                },
                new()
                {
                    Id = 9902,
                    CustomerId = 102,
                    EmployeeId = 201,
                    Subject = "Support Ticket for Customer 102",
                    Status = TicketStatus.Open,
                    Messages = []
                }
            };

            var json = JsonSerializer.Serialize(defaultTickets, _jsonOptions);
            File.WriteAllText(_supportTicketsFilePath, json);
        }
    }
}