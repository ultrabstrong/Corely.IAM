using Bunit;
using Corely.IAM.Web.Components.Shared;

namespace Corely.IAM.Web.UnitTests.Components.Shared;

public class LocalTimeTests : BunitContext
{
    private static readonly DateTime Moment = new(2026, 11, 4, 4, 20, 55, DateTimeKind.Utc);

    [Fact]
    public void LocalTime_ShowsTheBrowsersTime_WithItsZone()
    {
        JSInterop
            .SetupModule("./_content/Corely.IAM.Web/js/local-time.js")
            .Setup<string?>("format", "2026-11-04T04:20:55Z", false, false)
            .SetResult("Nov 3, 2026, 9:20 PM MST");

        var cut = Render<LocalTime>(p => p.Add(x => x.Value, Moment));

        cut.WaitForAssertion(() =>
            Assert.Equal("Nov 3, 2026, 9:20 PM MST", cut.Find("time").TextContent)
        );
        Assert.Equal("2026-11-04T04:20:55Z", cut.Find("time").GetAttribute("datetime"));
    }

    [Fact]
    public void LocalTime_NamesUtc_UntilTheBrowserAnswers()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = Render<LocalTime>(p => p.Add(x => x.Value, Moment).Add(x => x.Seconds, true));

        Assert.Equal("2026-11-04 04:20:55 UTC", cut.Find("time").TextContent);
    }

    [Fact]
    public void LocalTime_NamesNoZone_ForABareDate()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = Render<LocalTime>(p => p.Add(x => x.Value, Moment).Add(x => x.DateOnly, true));

        Assert.Equal("2026-11-04", cut.Find("time").TextContent);
    }

    [Fact]
    public void LocalTime_ShowsItsEmptyText_WithoutAValue()
    {
        var cut = Render<LocalTime>(p =>
            p.Add(x => x.Value, (DateTime?)null).Add(x => x.Empty, "Never")
        );

        Assert.Equal("Never", cut.Markup.Trim());
    }
}
