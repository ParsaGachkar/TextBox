using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace TextBox.Sdk;

/// <summary>
/// Optional API-key auth for the NSwag-generated client. Set <see cref="ApiKey"/>
/// to send <c>Authorization: Bearer &lt;key&gt;</c>; leave it unset for open mocks.
/// </summary>
public partial class TextBoxClient
{
    /// <summary>API key for <c>POST /api/messages</c>. Null or empty means unauthenticated.</summary>
    public string? ApiKey { get; set; }

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url) =>
        ApplyApiKey(request);

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, StringBuilder urlBuilder) =>
        ApplyApiKey(request);

    private void ApplyApiKey(HttpRequestMessage request)
    {
        if (string.IsNullOrEmpty(ApiKey))
            return;
        if (request.Headers.Contains("Authorization"))
            return;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
    }
}
