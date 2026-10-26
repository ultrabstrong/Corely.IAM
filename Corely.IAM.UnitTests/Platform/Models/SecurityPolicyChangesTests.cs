using Corely.IAM.Platform.Models;

namespace Corely.IAM.UnitTests.Platform.Models;

public class SecurityPolicyChangesTests
{
    private static readonly SecurityPolicy _base = SecurityPolicy.Default;

    [Fact]
    public void ChangesTo_ReturnsNothing_WhenNothingChanged()
    {
        Assert.Empty(_base.ChangesTo(_base with { }));
    }

    [Theory]
    [InlineData(6, true)]
    [InlineData(12, false)]
    public void ChangesTo_MinimumLength_WeakensOnlyWhenLowered(int value, bool weakens)
    {
        AssertSingle(_base with { PasswordMinimumLength = value }, weakens);
    }

    [Theory]
    [InlineData(64)]
    [InlineData(200)]
    public void ChangesTo_MaximumLength_NeverWeakens(int value)
    {
        AssertSingle(_base with { PasswordMaximumLength = value }, false);
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(3, false)]
    public void ChangesTo_MaxLoginAttempts_WeakensOnlyWhenRaised(int value, bool weakens)
    {
        AssertSingle(_base with { MaxLoginAttempts = value }, weakens);
    }

    [Theory]
    [InlineData(60, true)]
    [InlineData(3600, false)]
    public void ChangesTo_LockoutCooldown_WeakensOnlyWhenShortened(int value, bool weakens)
    {
        AssertSingle(_base with { LockoutCooldownSeconds = value }, weakens);
    }

    [Theory]
    [InlineData(7200, true)]
    [InlineData(600, false)]
    public void ChangesTo_TokenLifetime_WeakensOnlyWhenLengthened(int value, bool weakens)
    {
        AssertSingle(_base with { AuthTokenTtlSeconds = value }, weakens);
    }

    [Theory]
    [InlineData(1209600, true)]
    [InlineData(7200, false)]
    public void ChangesTo_SessionLifetime_WeakensOnlyWhenLengthened(int value, bool weakens)
    {
        AssertSingle(_base with { AuthSessionTtlSeconds = value }, weakens);
    }

    [Theory]
    [InlineData(172800, true)]
    [InlineData(3600, false)]
    public void ChangesTo_IdleTimeout_WeakensOnlyWhenLengthened(int value, bool weakens)
    {
        AssertSingle(_base with { AuthSessionIdleTimeoutSeconds = value }, weakens);
    }

    [Theory]
    [InlineData(60, true)]
    [InlineData(0, false)]
    public void ChangesTo_RefreshReuseGrace_WeakensOnlyWhenLengthened(int value, bool weakens)
    {
        AssertSingle(_base with { RefreshReuseGraceSeconds = value }, weakens);
    }

    [Theory]
    [InlineData(600)]
    [InlineData(120)]
    public void ChangesTo_MfaChallengeTimeout_NeverWeakens(int value)
    {
        AssertSingle(_base with { MfaChallengeTimeoutSeconds = value }, false);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(5)]
    public void ChangesTo_RecoveryCodeCount_NeverWeakens(int value)
    {
        AssertSingle(_base with { TotpRecoveryCodeCount = value }, false);
    }

    [Fact]
    public void ChangesTo_DroppingUppercase_Weakens()
    {
        AssertDropped(_base with { RequireUppercase = false });
    }

    [Fact]
    public void ChangesTo_DroppingLowercase_Weakens()
    {
        AssertDropped(_base with { RequireLowercase = false });
    }

    [Fact]
    public void ChangesTo_DroppingDigit_Weakens()
    {
        AssertDropped(_base with { RequireDigit = false });
    }

    [Fact]
    public void ChangesTo_DroppingSymbol_Weakens()
    {
        var before = _base with { RequireNonAlphanumeric = true };

        AssertDropped(before, before with { RequireNonAlphanumeric = false });
    }

    [Fact]
    public void ChangesTo_AddingSymbolRule_DoesNotWeaken()
    {
        var change = Assert.Single(_base.ChangesTo(_base with { RequireNonAlphanumeric = true }));

        Assert.False(change.Weakens);
        Assert.Equal(false, change.OldValue);
        Assert.Equal(true, change.NewValue);
    }

    [Fact]
    public void ChangesTo_ListsWeakeningChangesFirst()
    {
        var updated = _base with
        {
            PasswordMaximumLength = 200,
            MaxLoginAttempts = 10,
            TotpRecoveryCodeCount = 20,
            RequireDigit = false,
        };

        var changes = _base.ChangesTo(updated);

        Assert.Equal([true, true, false, false], changes.Select(c => c.Weakens));
    }

    private static void AssertSingle(SecurityPolicy updated, bool weakens)
    {
        var change = Assert.Single(_base.ChangesTo(updated));

        Assert.Equal(weakens, change.Weakens);
    }

    private static void AssertDropped(SecurityPolicy updated) => AssertDropped(_base, updated);

    private static void AssertDropped(SecurityPolicy before, SecurityPolicy updated)
    {
        var change = Assert.Single(before.ChangesTo(updated));

        Assert.True(change.Weakens);
        Assert.Equal(true, change.OldValue);
        Assert.Equal(false, change.NewValue);
    }
}
