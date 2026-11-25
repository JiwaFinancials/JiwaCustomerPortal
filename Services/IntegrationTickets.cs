using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace JiwaCustomerPortal.Services;

public sealed record IntegrationTicketAuthorization(string SessionId, string DebtorId, string ContactNameId,
    JsonElement Context, DateTimeOffset ExpiresAt);

public sealed class IntegrationTickets
{
    private const int MaximumContextSize = 4096;
    private readonly object sync = new();
    private readonly Dictionary<string, IntegrationTicketAuthorization> tickets = new(StringComparer.Ordinal);
    private readonly TimeProvider clock;
    private readonly byte[]? redemptionSecretHash;

    public IntegrationTickets(IConfiguration configuration, TimeProvider clock)
    {
        this.clock = clock;
        string? secret = configuration["IntegrationTicket:RedemptionSecret"];
        if (!string.IsNullOrEmpty(secret))
        {
            if (secret.Length < 32 || secret.Any(char.IsWhiteSpace))
                throw new InvalidOperationException("IntegrationTicket:RedemptionSecret must contain at least 32 characters and no whitespace.");
            redemptionSecretHash = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        }
    }

    public bool AuthorizeRedemption(string authorization)
    {
        if (redemptionSecretHash == null || !authorization.StartsWith("Bearer ", StringComparison.Ordinal)) return false;
        byte[] suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(authorization[7..]));
        return CryptographicOperations.FixedTimeEquals(redemptionSecretHash, suppliedHash);
    }

    public void ValidateConfiguration()
    {
        if (redemptionSecretHash == null)
            throw new InvalidOperationException("Integration tickets are not configured. Configure IntegrationTicket:RedemptionSecret on the portal.");
    }

    public JsonElement ParseContext(string? contextJson)
    {
        if (contextJson != null && Encoding.UTF8.GetByteCount(contextJson) > MaximumContextSize)
            throw new InvalidOperationException("Ticket context must be a JSON object no larger than 4096 bytes.");
        if (string.IsNullOrWhiteSpace(contextJson))
            return JsonSerializer.SerializeToElement(new Dictionary<string, object>());

        try
        {
            using JsonDocument document = JsonDocument.Parse(contextJson);
            return ValidateContext(document.RootElement);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Ticket context must be a valid JSON object.", ex);
        }
    }

    public string Issue(IntegrationAuthenticationResult result, JsonElement context)
    {
        ValidateConfiguration();
        context = ValidateContext(context);
        if (!result.Success || string.IsNullOrWhiteSpace(result.SessionId) || result.Debtor == null ||
            string.IsNullOrWhiteSpace(result.Debtor.Id) || string.IsNullOrWhiteSpace(result.Debtor.ContactNameId))
            throw new InvalidOperationException("Integration tickets require a freshly authenticated debtor contact.");

        lock (sync)
        {
            var now = clock.GetUtcNow();
            foreach (string expired in tickets.Where(x => x.Value.ExpiresAt <= now).Select(x => x.Key).ToArray())
                tickets.Remove(expired);
            if (tickets.Count >= 1000)
                throw new InvalidOperationException("Too many integration ticket requests are pending. Please try again shortly.");

            string ticket = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            tickets.Add(ticket, new(result.SessionId, result.Debtor.Id.TrimEnd(), result.Debtor.ContactNameId.TrimEnd(),
                context, now.AddMinutes(2)));
            return ticket;
        }
    }

    public IntegrationTicketAuthorization Take(string? ticket)
    {
        lock (sync)
        {
            if (ticket == null || ticket.Length != 64 || !tickets.Remove(ticket, out var authorization) ||
                authorization.ExpiresAt <= clock.GetUtcNow())
                throw new InvalidOperationException("Invalid, expired or already used integration ticket. Sign in again.");
            return authorization;
        }
    }

    private static JsonElement ValidateContext(JsonElement context)
    {
        if (context.ValueKind != JsonValueKind.Object || Encoding.UTF8.GetByteCount(context.GetRawText()) > MaximumContextSize)
            throw new InvalidOperationException("Ticket context must be a JSON object no larger than 4096 bytes.");

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty property in context.EnumerateObject())
        {
            if (property.Name.Length is 0 or > 64 || !names.Add(property.Name) ||
                property.Name.Equals("DebtorId", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Equals("ContactNameId", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Equals("SessionId", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Equals("IntegrationTicket", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Equals("Context", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Ticket context contains an invalid or reserved property name.");
        }

        return context.Clone();
    }
}
