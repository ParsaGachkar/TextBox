namespace TextBox.Sdk;

/// <summary>
/// Configuration for <see cref="TextBoxClient"/>. The API key is optional:
/// leave it unset when the server has no key configured (open mock mode).
/// </summary>
public sealed class TextBoxOptions
{
    /// <summary>Server base address (e.g. <c>http://localhost:5031</c> for local runs, <c>http://localhost:8080</c> for Docker).</summary>
    public string BaseAddress { get; set; } = "http://localhost:5031";

    /// <summary>API key for <c>POST /api/messages</c>. Null or empty means unauthenticated.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Per-request timeout applied to internally created <see cref="System.Net.Http.HttpClient"/> instances.</summary>
    public System.TimeSpan Timeout { get; set; } = System.TimeSpan.FromSeconds(100);
}
