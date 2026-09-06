using Bunit;
using TextBox.Components.Layout;

namespace TextBox.Tests;

public class SharedLayoutTests : BunitContext
{
    [Fact]
    public void TopNavbar_HasBrandSdkAndDocsLinks()
    {
        var cut = Render<TopNavbar>();

        Assert.Contains("TextBox", cut.Markup);
        var sdk = cut.Find("a[href='sdk']");
        Assert.Equal("_blank", sdk.GetAttribute("target"));
        var docs = cut.Find("a[href='scalar']");
        Assert.Equal("_blank", docs.GetAttribute("target"));
    }

    [Fact]
    public void DocsLayout_RendersNavbarAndBody()
    {
        var cut = Render<DocsLayout>(p => p.Add(
            l => l.Body,
            (Microsoft.AspNetCore.Components.RenderFragment)(b => b.AddContent(0, "docs-body-probe"))));

        Assert.Contains("docs-body-probe", cut.Markup);
        Assert.NotNull(cut.Find("a[href='sdk']"));
        Assert.NotNull(cut.Find("main"));
    }
}
