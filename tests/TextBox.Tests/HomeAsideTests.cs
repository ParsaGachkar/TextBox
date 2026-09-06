using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TextBox.Auth;
using TextBox.Components.Layout;
using TextBox.Services;

namespace TextBox.Tests;

public class HomeAsideTests : BunitContext
{
    public HomeAsideTests()
    {
        Services.AddSingleton<ApiKeyService>();
    }

    private void GivenKey(string? key) =>
        Services.Configure<ApiKeyAuthOptions>(ApiKeyAuthOptions.SchemeName, o => o.Key = key);

    [Fact]
    public void ShowsBearerHeader_WhenKeyConfigured()
    {
        GivenKey("secret-abc");

        var cut = Render<HomeAside>();

        Assert.Contains("API key", cut.Markup);
        Assert.Contains("Authorization: Bearer secret-abc", cut.Markup);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShowsOpenState_WhenNoKeyConfigured(string? key)
    {
        GivenKey(key);

        var cut = Render<HomeAside>();

        Assert.Contains("No key configured", cut.Markup);
        Assert.DoesNotContain("Bearer", cut.Markup);
    }

    [Fact]
    public void ShowsGuide_WithDocsLink()
    {
        GivenKey(null);

        var cut = Render<HomeAside>();

        Assert.Contains("Quick guide", cut.Markup);
        var link = cut.Find("a.link");
        Assert.Equal("scalar", link.GetAttribute("href"));
    }

    [Fact]
    public void ShowsSdkCard_WithInstallAndGuideLink()
    {
        GivenKey(null);

        var cut = Render<HomeAside>();

        Assert.Contains(".NET SDK", cut.Markup);
        Assert.Contains("dotnet add package TextBox.Sdk", cut.Markup);
        var sdk = cut.Find("a[href='sdk']");
        Assert.Equal("_blank", sdk.GetAttribute("target"));
    }

    [Fact]
    public void CopyButton_CopiesRawKey()
    {
        GivenKey("secret-abc");
        JSInterop.SetupVoid("textBoxClipboard.copy", "secret-abc").SetVoidResult();
        var cut = Render<HomeAside>();

        cut.Find("button[aria-label='Copy API key']").Click();

        JSInterop.VerifyInvoke("textBoxClipboard.copy", "expected raw key to be copied");
        cut.WaitForAssertion(() =>
            Assert.Contains("M20 6", cut.Find("button[aria-label='Copy API key']").InnerHtml));
    }
}
