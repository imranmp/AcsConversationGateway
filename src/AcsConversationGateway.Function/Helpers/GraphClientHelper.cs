using Azure.Identity;
using Microsoft.Graph;

namespace AcsConversationGateway.Function.Helpers;

public static class GraphClientHelper
{
    public static GraphServiceClient CreateGraphClient(IConfiguration configuration)
    {
        var clientId = configuration["Graph:ClientId"];
        var clientSecret = configuration["Graph:ClientSecret"];
        var tenantId = configuration["Graph:TenantId"];

        var scopes = new[] { "https://graph.microsoft.com/.default" };

        var options = new ClientSecretCredentialOptions
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
        };

        var clientSecretCredential = new ClientSecretCredential(
            tenantId, clientId, clientSecret, options);

        return new GraphServiceClient(clientSecretCredential, scopes);
    }
}