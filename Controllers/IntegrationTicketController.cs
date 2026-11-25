using JiwaCustomerPortal.Services;
using JiwaFinancials.Jiwa.JiwaServiceModel;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebServiceException = ServiceStack.WebServiceException;

namespace JiwaCustomerPortal.Controllers;

[ApiController]
[Route("integration/ticket/redeem")]
public sealed class IntegrationTicketController(IntegrationTickets tickets, ILogger<IntegrationTicketController> logger) : ControllerBase
{
    public sealed record RedemptionRequest(string? IntegrationTicket);

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [RequestSizeLimit(1024)]
    public async Task<IActionResult> Redeem([FromBody] RedemptionRequest request, CancellationToken cancellationToken)
    {
        if (!tickets.AuthorizeRedemption(Request.Headers.Authorization.ToString()))
        {
            logger.LogWarning("Unauthorized integration ticket redemption.");
            return Unauthorized(new { error = "Unauthorized integration ticket redemption." });
        }

        IntegrationTicketAuthorization authorization;
        try
        {
            authorization = tickets.Take(request.IntegrationTicket);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Invalid, expired or replayed integration ticket.");
            return BadRequest(new { error = ex.Message });
        }

        try
        {
            var session = await JiwaAPI.GetAsync(new AuthCurrentSessionGETRequest(),
                jiwaAPISessionId: authorization.SessionId, cancellationToken: cancellationToken);
            if ((session.AuthProvider != "debtorcontactnameemailaddresscredentials" && session.AuthProvider != "debtorcontactnameidcredentials") ||
                session.DebtorID?.TrimEnd() != authorization.DebtorId || session.DebtorContactNameID?.TrimEnd() != authorization.ContactNameId)
            {
                logger.LogWarning("Integration ticket session identity is no longer valid.");
                return Unauthorized(new { error = "Customer session is no longer valid. Sign in again." });
            }

            var roles = await JiwaAPI.GetAsync(new DebtorContactNameCustomerWebPortalRoleGETRequest(),
                jiwaAPISessionId: authorization.SessionId, cancellationToken: cancellationToken);
            if (roles.Roles == null || (!roles.Roles.Contains("User") && !roles.Roles.Contains("Admin")))
            {
                logger.LogWarning("Integration ticket customer no longer has a portal role.");
                return StatusCode(403, new { error = "Customer no longer has permission to use integration tickets." });
            }
        }
        catch (WebServiceException ex) when (ex.StatusCode is 401 or 403)
        {
            logger.LogWarning("Integration ticket session validation was denied by Jiwa (HTTP {StatusCode}).", ex.StatusCode);
            return Unauthorized(new { error = "Customer session is no longer authorized. Sign in again." });
        }

        var response = new Dictionary<string, object?>
        {
            ["DebtorId"] = authorization.DebtorId,
            ["ContactNameId"] = authorization.ContactNameId
        };
        foreach (JsonProperty property in authorization.Context.EnumerateObject())
            response[property.Name] = property.Value;
        return Ok(response);
    }
}
