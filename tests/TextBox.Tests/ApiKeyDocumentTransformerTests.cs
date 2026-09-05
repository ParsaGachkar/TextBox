using System.Net.Http;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using NSubstitute;
using TextBox.Auth;
using TextBox.Endpoints;

namespace TextBox.Tests;

public class ApiKeyDocumentTransformerTests
{
    private static OpenApiDocument DocumentWithSendEndpoint()
    {
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths(),
            Components = new OpenApiComponents()
        };
        var item = new OpenApiPathItem();
        item.AddOperation(HttpMethod.Post, new OpenApiOperation());
        item.AddOperation(HttpMethod.Get, new OpenApiOperation());
        document.Paths.Add("/api/messages", item);
        return document;
    }

    private static ApiKeyDocumentTransformer Transformer(string? key)
    {
        var monitor = Substitute.For<IOptionsMonitor<ApiKeyAuthOptions>>();
        monitor.Get(ApiKeyAuthOptions.SchemeName).Returns(new ApiKeyAuthOptions { Key = key });
        return new ApiKeyDocumentTransformer(new ApiKeyService(monitor));
    }

    [Fact]
    public async Task KeyConfigured_AddsBearerScheme_AndSecuresSendEndpointOnly()
    {
        var document = DocumentWithSendEndpoint();

        // Context is unused by this transformer.
        await Transformer("secret").TransformAsync(document, null!, CancellationToken.None);

        var components = document.Components!;
        var scheme = Assert.IsType<OpenApiSecurityScheme>(components.SecuritySchemes!["ApiKey"]);
        Assert.Equal(SecuritySchemeType.Http, scheme.Type);
        Assert.Equal("bearer", scheme.Scheme);

        var path = document.Paths!["/api/messages"]!;
        var operations = path.Operations!;
        var send = operations[HttpMethod.Post];
        var requirement = Assert.Single(send.Security!);
        Assert.Single(requirement);

        var list = operations[HttpMethod.Get];
        Assert.True(list.Security is null || list.Security.Count == 0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task NoKeyConfigured_LeavesDocumentUntouched(string? key)
    {
        var document = DocumentWithSendEndpoint();

        await Transformer(key).TransformAsync(document, null!, CancellationToken.None);

        Assert.True(document.Components!.SecuritySchemes is null ||
            !document.Components.SecuritySchemes.ContainsKey("ApiKey"));
        var get = document.Paths!["/api/messages"]!.Operations![HttpMethod.Get];
        Assert.True(get.Security is null || get.Security.Count == 0);
    }
}
