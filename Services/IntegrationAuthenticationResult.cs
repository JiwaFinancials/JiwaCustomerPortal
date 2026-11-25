using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace JiwaCustomerPortal.Services;

public sealed record IntegrationDebtor(string Id, string Name, string? ContactNameId);

public sealed record IntegrationAuthenticationResult(bool Success, string? ErrorMessage, string? SessionId, IntegrationDebtor? Debtor,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] string? IntegrationTicket = null);

public sealed class IntegrationAuthenticationResultProtector
{
    private readonly ITimeLimitedDataProtector protector;

    public IntegrationAuthenticationResultProtector(IDataProtectionProvider provider)
    {
        protector = provider.CreateProtector("JiwaCustomerPortal.IntegrationAuthenticationResult.v1").ToTimeLimitedDataProtector();
    }

    public string Protect(IntegrationAuthenticationResult result) =>
        protector.Protect(JsonSerializer.Serialize(result), TimeSpan.FromMinutes(2));

    public IntegrationAuthenticationResult Unprotect(string token) =>
        JsonSerializer.Deserialize<IntegrationAuthenticationResult>(protector.Unprotect(token))
        ?? throw new CryptographicException("Invalid authentication result.");
}
