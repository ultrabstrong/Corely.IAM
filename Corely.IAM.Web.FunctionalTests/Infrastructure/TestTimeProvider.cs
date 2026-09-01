namespace Corely.IAM.Web.FunctionalTests.Infrastructure;

public sealed class TestTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _utcNow = start;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan by) => _utcNow = _utcNow.Add(by);

    public void AdvanceSeconds(int seconds) => Advance(TimeSpan.FromSeconds(seconds));
}
