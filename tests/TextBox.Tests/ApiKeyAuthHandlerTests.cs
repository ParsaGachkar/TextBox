using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using TextBox.Auth;

namespace TextBox.Tests;

public class ApiKeyAuthHandlerTests
{
    private static ApiKeyAuthHandler Handler(string? key)
    {
        var monitor = Substitute.For<IOptionsMonitor<ApiKeyAuthOptions>>();
        var options = new ApiKeyAuthOptions { Key = key };
        monitor.CurrentValue.Returns(options);
        monitor.Get(Arg.Any<string>()).Returns(options);
        return new ApiKeyAuthHandler(monitor, NullLoggerFactory.Instance, UrlEncoder.Default);
    }

    private static async Task<AuthenticateResult> Authenticate(ApiKeyAuthHandler handler, string? header)
    {
        var context = new DefaultHttpContext();
        if (header is not null)
            context.Request.Headers.Authorization = header;
        var scheme = new AuthenticationScheme(ApiKeyAuthOptions.SchemeName, null, typeof(ApiKeyAuthHandler));
        await handler.InitializeAsync(scheme, context);
        return await handler.AuthenticateAsync();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NoKeyConfigured_ReturnsNoResult(string? key)
    {
        var result = await Authenticate(Handler(key), null);

        Assert.False(result.Succeeded);
        Assert.Null(result.Principal);
    }

    [Theory]
    [InlineData("Bearer secret-1")]
    [InlineData("bearer secret-1")]
    public async Task ValidKey_SucceedsWithApiKeyIdentity(string header)
    {
        var result = await Authenticate(Handler("secret-1"), header);

        Assert.True(result.Succeeded);
        Assert.Equal("api-key", result.Principal?.Identity?.Name);
        Assert.True(result.Principal?.Identity?.IsAuthenticated);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong")]
    [InlineData("secret-1")]
    [InlineData("Bearer Secret-1")]
    [InlineData("Bearer ")]
    [InlineData("Basic secret-1")]
    public async Task InvalidKey_Fails(string? header)
    {
        var result = await Authenticate(Handler("secret-1"), header);

        Assert.False(result.Succeeded);
        Assert.Equal("Missing or invalid API key.", result.Failure?.Message);
    }
}
