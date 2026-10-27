using Corely.IAM.Audits.Enums;
using Corely.IAM.Audits.Models;
using Corely.IAM.Platform.Models;

namespace Corely.IAM.UnitTests.Platform.Models;

public class PlatformSettingsChangesTests
{
    private static readonly PlatformSettings _base = PlatformSettings.Default;

    [Fact]
    public void ChangesTo_ReturnsNothing_WhenNothingChanged()
    {
        Assert.Empty(_base.ChangesTo(_base with { }));
    }

    [Fact]
    public void ChangesTo_TurningAuditingOff_Weakens()
    {
        var change = Assert.Single(_base.ChangesTo(_base with { AuditEnabled = false }));

        Assert.True(change.Weakens);
        Assert.Equal(true, change.OldValue);
        Assert.Equal(false, change.NewValue);
    }

    [Fact]
    public void ChangesTo_TurningAuditingOn_DoesNotWeaken()
    {
        var off = _base with { AuditEnabled = false };

        Assert.False(Assert.Single(off.ChangesTo(_base)).Weakens);
    }

    [Theory]
    [InlineData(30, true)]
    [InlineData(400, false)]
    public void ChangesTo_Retention_WeakensOnlyWhenShortened(int days, bool weakens)
    {
        var change = Assert.Single(_base.ChangesTo(_base with { AuditMaxRetentionDays = days }));

        Assert.Equal(weakens, change.Weakens);
    }

    [Fact]
    public void ChangesTo_AllowedActions_DroppingAFlagWeakens()
    {
        var change = Assert.Single(
            _base.ChangesTo(_base with { AuditAllowedActions = AuditActions.Create })
        );

        Assert.True(change.Weakens);
    }

    [Fact]
    public void ChangesTo_AllowedActions_AddingAFlagDoesNotWeaken()
    {
        var before = _base with { AuditAllowedActions = AuditActions.Create };

        var change = Assert.Single(before.ChangesTo(_base));

        Assert.False(change.Weakens);
    }

    [Fact]
    public void ChangesTo_SystemContextActions_AddingAFlagDoesNotWeaken()
    {
        var change = Assert.Single(
            _base.ChangesTo(_base with { SystemContextActions = AuditActions.Read })
        );

        Assert.False(change.Weakens);
    }

    [Fact]
    public void ChangesTo_SystemContextActions_DroppingAFlagWeakens()
    {
        var before = _base with { SystemContextActions = AuditActions.Read | AuditActions.Delete };

        var change = Assert.Single(
            before.ChangesTo(before with { SystemContextActions = AuditActions.Read })
        );

        Assert.True(change.Weakens);
    }

    [Fact]
    public void ChangesTo_AccountlessActions_DroppingAFlagWeakens()
    {
        var change = Assert.Single(
            _base.ChangesTo(_base with { AccountlessActions = AuditActions.Create })
        );

        Assert.True(change.Weakens);
    }

    [Fact]
    public void ChangesTo_AccountlessActions_SwappingAFlagWeakens()
    {
        var change = Assert.Single(
            _base.ChangesTo(
                _base with
                {
                    AccountlessActions =
                        AuditActions.Create
                        | AuditActions.Update
                        | AuditActions.Delete
                        | AuditActions.Read,
                }
            )
        );

        Assert.True(change.Weakens);
    }

    [Fact]
    public void ChangesTo_ListsWeakeningChangesFirst()
    {
        var updated = _base with { AuditMaxRetentionDays = 500, AuditEnabled = false };

        var changes = _base.ChangesTo(updated);

        Assert.Equal([true, false], changes.Select(c => c.Weakens));
    }
}
