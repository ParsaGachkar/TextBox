using System.Net.Http;

namespace TextBox.Sdk;

/// <summary>
/// DI-free construction for the NSwag-generated client.
/// </summary>
public partial class TextBoxClient
{
    /// <summary>Creates a client from options, owning its <see cref="HttpClient"/>.</summary>
    public TextBoxClient(TextBoxOptions options)
        : this(CreateHttpClient(options, null))
    {
        BaseUrl = options.BaseAddress;
        ApiKey = options.ApiKey;
    }

    /// <summary>Creates a client from options over a custom handler (other containers, tests).</summary>
    public TextBoxClient(TextBoxOptions options, HttpMessageHandler? handler)
        : this(CreateHttpClient(options, handler))
    {
        BaseUrl = options.BaseAddress;
        ApiKey = options.ApiKey;
    }

    /// <summary>Creates an unauthenticated client for the given base address.</summary>
    public TextBoxClient(string baseAddress)
        : this(new TextBoxOptions { BaseAddress = baseAddress })
    {
    }

    /// <summary>Creates a client for the given base address, optionally authenticated.</summary>
    public TextBoxClient(string baseAddress, string? apiKey)
        : this(new TextBoxOptions { BaseAddress = baseAddress, ApiKey = apiKey })
    {
    }

    private static HttpClient CreateHttpClient(TextBoxOptions options, HttpMessageHandler? handler)
    {
        var client = handler is null
            ? new HttpClient()
            : new HttpClient(handler, disposeHandler: false);
        client.Timeout = options.Timeout;
        return client;
    }
}
