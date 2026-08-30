namespace Corely.IAM.Web.FunctionalTests.Infrastructure;

/// <summary>
/// Controllable clock for functional tests. Token expiry, session caps, and cookie lifetimes are
/// all wall-clock dependent, so tests advance this rather than sleeping - a seven day session
/// expiry must be assertable in a millisecond.
/// </summary>
public sealed class TestTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _utcNow = start;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan by) => _utcNow = _utcNow.Add(by);

    public void AdvanceSeconds(int seconds) => Advance(TimeSpan.FromSeconds(seconds));
}
