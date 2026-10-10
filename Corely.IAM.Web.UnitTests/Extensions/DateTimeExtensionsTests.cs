using Corely.IAM.Web.Extensions;

namespace Corely.IAM.Web.UnitTests.Extensions;

public class DateTimeExtensionsTests
{
    private static readonly DateTime _date = new(2026, 3, 4, 15, 30, 0, DateTimeKind.Unspecified);

    [Fact]
    public void StartOfDayUtc_IsMidnightUtc()
    {
        var start = _date.StartOfDayUtc();

        Assert.Equal(new DateTime(2026, 3, 4, 0, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(DateTimeKind.Utc, start.Kind);
    }

    [Fact]
    public void EndOfDayUtc_IsTheLastTickOfTheDay()
    {
        Assert.Equal(
            new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc).AddTicks(-1),
            _date.EndOfDayUtc()
        );
    }
}
