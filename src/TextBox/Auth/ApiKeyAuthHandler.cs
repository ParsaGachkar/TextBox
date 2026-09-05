using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TextBox.Auth;

/// <summary>
/// Validates <c>Authorization: Bearer &lt;key&gt;</c> against
/// <see cref="ApiKeyAuthOptions"/> using constant-time comparison.
/// </summary>
public sealed class ApiKeyAuthHandler(
    IOptionsMonitor<ApiKeyAuthOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<ApiKeyAuthOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var key = Options.Key;
        if (string.IsNullOrWhiteSpace(key))
            return Task.FromResult(AuthenticateResult.NoResult());

        const string prefix = "Bearer ";
        var header = Request.Headers.Authorization.FirstOrDefault();
        if (header is null || !header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.Fail("Missing or invalid API key."));

        var provided = header[prefix.Length..].Trim();
        if (string.IsNullOrEmpty(provided) ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(provided)))
            return Task.FromResult(AuthenticateResult.Fail("Missing or invalid API key."));

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "api-key")], Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Response.WriteAsJsonAsync(new { error = "Missing or invalid API key." });
    }
}
