using AcsConversationGateway.Api;
using AcsConversationGateway.Api.Services;
using Scalar.AspNetCore;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Register data service
builder.Services.AddSingleton<IDataService, JsonDataService>();
builder.Services.AddSingleton<MessageProcessingService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure JSON options to serialize enums as strings
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Move endpoint registration to extension method
app.MapApiEndpoints();

app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options.Servers = []; // This disables server-based routing that causes CORS mismatches
    });

    app.UseCors("AllowAll");
}

app.Run();