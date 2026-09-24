using Bunit;
using Corely.IAM.Web.Components.Shared;

namespace Corely.IAM.Web.UnitTests.Components;

public class ThemeToggleTests : BunitContext
{
    [Fact]
    public void Render_OffersTheOtherTheme_ForTheThemeTheScriptReports()
    {
        JSInterop.Setup<string>("corelyTheme.current").SetResult("dark");

        var cut = Render<ThemeToggle>();

        cut.WaitForAssertion(() =>
            Assert.Equal("Use light theme", cut.Find("button").GetAttribute("aria-label"))
        );
        Assert.NotNull(cut.Find("i.bi-sun"));
    }

    [Fact]
    public void Click_TogglesThroughTheScript_AndLabelsTheNextTheme()
    {
        JSInterop.Setup<string>("corelyTheme.current").SetResult("light");
        var toggle = JSInterop.Setup<string>("corelyTheme.toggle").SetResult("dark");

        var cut = Render<ThemeToggle>();
        cut.WaitForAssertion(() =>
            Assert.Equal("Use dark theme", cut.Find("button").GetAttribute("aria-label"))
        );

        cut.Find("button").Click();

        Assert.Single(toggle.Invocations);
        cut.WaitForAssertion(() =>
            Assert.Equal("Use light theme", cut.Find("button").GetAttribute("aria-label"))
        );
    }
}
