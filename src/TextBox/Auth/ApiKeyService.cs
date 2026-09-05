using Microsoft.Extensions.Options;

namespace TextBox.Auth;

/// <summary>
/// Single source of truth for the configured API key. The auth scheme binds
/// NAMED options (<see cref="ApiKeyAuthOptions.SchemeName"/>); resolving
/// plain <c>IOptions&lt;&gt;</c> (the default instance) silently yields empty,
/// which once made the dashboard show "open" while the API enforced a key.
/// Always go through here.
/// </summary>
public sealed class ApiKeyService(IOptionsMonitor<ApiKeyAuthOptions> monitor)
{
    public string? Key => monitor.Get(ApiKeyAuthOptions.SchemeName).Key;

    public bool IsEnabled => !string.IsNullOrWhiteSpace(Key);
}
