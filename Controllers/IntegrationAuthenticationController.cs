using System.Security.Cryptography;
using JiwaCustomerPortal.Services;
using Microsoft.AspNetCore.Mvc;

namespace JiwaCustomerPortal.Controllers;

[ApiController]
[Route("integration/auth/result")]
public sealed class IntegrationAuthenticationController : ControllerBase
{
    private readonly IntegrationAuthenticationResultProtector protector;

    public IntegrationAuthenticationController(IntegrationAuthenticationResultProtector protector)
    {
        this.protector = protector;
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [RequestSizeLimit(16384)]
    public IActionResult Result([FromForm] string? token)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.XContentTypeOptions = "nosniff";

        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new IntegrationAuthenticationResult(false, "Missing authentication result.", null, null));
        }

        try
        {
            return Ok(protector.Unprotect(token));
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            return BadRequest(new IntegrationAuthenticationResult(false, "Invalid or expired authentication result.", null, null));
        }
    }
}
