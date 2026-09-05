using Microsoft.AspNetCore.Authentication;

namespace TextBox.Auth;

/// <summary>
/// API-key auth for sending SMS. Bind from the <c>ApiKey</c> section of
/// <c>appsettings.json</c>. Leave <see cref="Key"/> empty to leave the API
/// open; set it to require the key on <c>POST /api/messages</c>.
/// </summary>
public sealed class ApiKeyAuthOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "ApiKey";

    public const string SectionName = "ApiKey";

    public string? Key { get; set; }
}
