using Microsoft.Extensions.DependencyInjection;

namespace TextBox.Sdk;

/// <summary>
/// Optional Microsoft DI registration. The client works without any container
/// via <c>new TextBoxClient(...)</c>; this is convenience only.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers <see cref="ITextBoxClient"/> as a typed <see cref="System.Net.Http.HttpClient"/>.</summary>
    public static IHttpClientBuilder AddTextBoxClient(
        this IServiceCollection services,
        TextBoxOptions options)
    {
        return services
            .AddHttpClient<ITextBoxClient>()
            .AddTypedClient((http, _) =>
            {
                http.Timeout = options.Timeout;
                return (ITextBoxClient)new TextBoxClient(http)
                {
                    BaseUrl = options.BaseAddress,
                    ApiKey = options.ApiKey,
                };
            });
    }

    /// <summary>Registers <see cref="ITextBoxClient"/> for the given base address, optionally authenticated.</summary>
    public static IHttpClientBuilder AddTextBoxClient(
        this IServiceCollection services,
        string baseAddress,
        string? apiKey = null)
    {
        return services.AddTextBoxClient(new TextBoxOptions { BaseAddress = baseAddress, ApiKey = apiKey });
    }
}
