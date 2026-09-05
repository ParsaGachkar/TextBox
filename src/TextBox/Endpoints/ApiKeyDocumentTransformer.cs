using System.Net.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using TextBox.Auth;

namespace TextBox.Endpoints;

/// <summary>
/// Adds the Bearer API-key scheme to the OpenAPI document and marks
/// <c>POST /api/messages</c> as requiring it, so Scalar renders the
/// auth input for the send endpoint. Skips everything while no key is
/// configured (API open), keeping the doc honest in both modes.
/// </summary>
public sealed class ApiKeyDocumentTransformer(ApiKeyService apiKey) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (!apiKey.IsEnabled)
            return Task.CompletedTask;

        var components = document.Components ??= new OpenApiComponents();
        var schemes = components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        schemes["ApiKey"] = new OpenApiSecurityScheme
        {
            Description = "API key for sending SMS. Use: Bearer <key>.",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer"
        };

        var paths = document.Paths;
        if (paths is null)
            return Task.CompletedTask;
        if (!paths.TryGetValue("/api/messages", out var messages) || messages is null)
            return Task.CompletedTask;
        var operations = messages.Operations;
        if (operations is null)
            return Task.CompletedTask;
        if (!operations.TryGetValue(HttpMethod.Post, out var send) || send is null)
            return Task.CompletedTask;

        send.Security ??= [];
        send.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("ApiKey", document, null!)] = []
        });

        return Task.CompletedTask;
    }
}
