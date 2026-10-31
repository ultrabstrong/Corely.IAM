using Corely.IAM.Testing;

namespace Corely.IAM.UnitTests.Testing;

public class CrossAccountCoverageTests
{
    private interface IWidgetService
    {
        Task GetAsync();
        Task DeleteAsync();
    }

    private static readonly Type[] Services = [typeof(IWidgetService)];
    private const string GET = "IWidgetService.GetAsync";
    private const string DELETE = "IWidgetService.DeleteAsync";

    private static Dictionary<string, string> Exempt(string method, string reason) =>
        new() { [method] = reason };

    [Fact]
    public void MethodsOf_ListsEveryMethod_ForAnInterface()
    {
        Assert.Equal([GET, DELETE], CrossAccountCoverage.MethodsOf(Services));
    }

    [Fact]
    public void Check_IsComplete_ForAnAttackAndAReasonedExemption()
    {
        var coverage = CrossAccountCoverage.Check(
            Services,
            [GET],
            Exempt(DELETE, "Runs as the system")
        );

        Assert.True(coverage.IsComplete, coverage.ToString());
    }

    [Fact]
    public void Check_NamesTheMethod_ForAMethodWithNeither()
    {
        var coverage = CrossAccountCoverage.Check(Services, [GET], Exempt("x", "y"));

        Assert.False(coverage.IsComplete);
        Assert.Equal([DELETE], coverage.Uncovered);
        Assert.Contains(DELETE, coverage.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Check_RefusesTheExemption_ForAnExemptionWithoutAReason(string reason)
    {
        var coverage = CrossAccountCoverage.Check(Services, [GET], Exempt(DELETE, reason));

        Assert.Equal([DELETE], coverage.Unreasoned);
    }

    [Fact]
    public void Check_ReportsItAsStale_ForACaseOfAMethodThatIsGone()
    {
        var coverage = CrossAccountCoverage.Check(
            Services,
            [GET, DELETE, "IOld.Gone"],
            Exempt(GET, "r")
        );

        Assert.Equal(["IOld.Gone"], coverage.Stale);
        Assert.Equal([GET], coverage.Duplicated);
    }

    [Fact]
    public async Task FindReachedAsync_NamesTheAttack_ForAnAttackThatGotThrough()
    {
        var reached = await CrossAccountCoverage.FindReachedAsync(
            new Dictionary<string, Func<Task<bool>>>
            {
                [GET] = () => Task.FromResult(true),
                [DELETE] = () => Task.FromResult(false),
            }
        );

        Assert.Equal([DELETE], reached);
    }
}
