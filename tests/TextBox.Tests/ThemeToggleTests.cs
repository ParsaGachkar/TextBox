using Bunit;
using TextBox.Components.Layout;

namespace TextBox.Tests;

public class ThemeToggleTests : BunitContext
{
    [Fact]
    public void RendersSun_WhenStoredThemeIsLight()
    {
        JSInterop.Setup<string>("textBoxTheme.get").SetResult("light");

        var cut = Render<ThemeToggle>();

        // Sun icon has the center circle; moon does not.
        Assert.Contains("cx=\"12\" cy=\"12\"", cut.Find("button").InnerHtml);
    }

    [Fact]
    public void RendersMoon_WhenStoredThemeIsDark()
    {
        JSInterop.Setup<string>("textBoxTheme.get").SetResult("dark");

        var cut = Render<ThemeToggle>();

        cut.WaitForAssertion(() =>
            Assert.Contains("M12 3a6", cut.Find("button").InnerHtml));
    }

    [Fact]
    public void Click_PersistsNewTheme()
    {
        JSInterop.Setup<string>("textBoxTheme.get").SetResult("light");
        JSInterop.SetupVoid("textBoxTheme.set", "dark");
        var cut = Render<ThemeToggle>();

        cut.Find("button").Click();

        var invocation = JSInterop.VerifyInvoke("textBoxTheme.set", "expected theme to be persisted on toggle");
        Assert.Equal("dark", Assert.Single(invocation.Arguments));
        cut.WaitForAssertion(() =>
            Assert.Contains("M12 3a6", cut.Find("button").InnerHtml));
    }
}
