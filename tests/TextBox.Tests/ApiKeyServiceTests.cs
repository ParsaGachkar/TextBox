using Microsoft.Extensions.Options;
using NSubstitute;
using TextBox.Auth;

namespace TextBox.Tests;

public class ApiKeyServiceTests
{
    private static ApiKeyService Service(string? namedKey)
    {
        var monitor = Substitute.For<IOptionsMonitor<ApiKeyAuthOptions>>();
        // Default (unnamed) instance stays empty, mirroring a real host where
        // only the named "ApiKey" instance is bound from configuration.
        monitor.Get(Arg.Is<string>(name => name != ApiKeyAuthOptions.SchemeName))
            .Returns(new ApiKeyAuthOptions());
        monitor.Get(ApiKeyAuthOptions.SchemeName)
            .Returns(new ApiKeyAuthOptions { Key = namedKey });
        return new ApiKeyService(monitor);
    }

    [Fact]
    public void ReadsKey_FromNamedInstance_NotDefault()
    {
        var service = Service("secret-abc");

        Assert.Equal("secret-abc", service.Key);
        Assert.True(service.IsEnabled);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyKey_MeansDisabled(string? key)
    {
        var service = Service(key);

        Assert.False(service.IsEnabled);
    }
}
