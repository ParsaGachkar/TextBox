using Bunit;

namespace TextBox.Tests;

public class SdkPageTests : BunitContext
{
    [Fact]
    public void RendersInstallAndUsage()
    {
        var cut = Render<Components.Pages.Sdk>();

        Assert.Contains(".NET SDK", cut.Markup);
        Assert.Contains("dotnet add package TextBox.Sdk", cut.Markup);
        Assert.Contains("TextBoxClient", cut.Markup);
        var scalar = cut.Find("a[href='scalar']");
        Assert.NotNull(scalar);
    }
}
