using Corely.IAM.Audits.Constants;
using Corely.IAM.Audits.Models;
using Corely.IAM.Platform.Models;

namespace Corely.IAM.UnitTests.Audits.Models;

public class AccountAuditSettingsTests
{
    private static readonly Guid _accountId = Guid.CreateVersion7();

    [Fact]
    public void Default_RecordsCreateUpdateDelete_ForBothCohorts()
    {
        var settings = AccountAuditSettings.Default(_accountId);

        var expected = AuditActions.Create | AuditActions.Update | AuditActions.Delete;
        Assert.Equal(expected, settings.AccountMemberActions);
        Assert.Equal(expected, settings.PlatformMemberActions);
        Assert.Equal(AuditConstants.DEFAULT_RETENTION_DAYS, settings.RetentionDays);
    }

    [Theory]
    [InlineData(AuditCohort.AccountMember, AuditActions.Create | AuditActions.Read)]
    [InlineData(AuditCohort.PlatformMember, AuditActions.Delete | AuditActions.Execute)]
    public void EffectiveActions_ReturnsCohortActions_WhenPlatformAllowsAll(
        AuditCohort cohort,
        AuditActions expected
    )
    {
        var settings = new AccountAuditSettings(
            _accountId,
            AuditActions.Create | AuditActions.Read,
            AuditActions.Delete | AuditActions.Execute,
            30
        );

        Assert.Equal(expected, settings.EffectiveActions(cohort, PlatformSettings.Default));
    }

    [Fact]
    public void EffectiveActions_DropsActionsThePlatformForbids()
    {
        var settings = new AccountAuditSettings(_accountId, AuditActions.All, AuditActions.All, 30);
        var platform = PlatformSettings.Default with
        {
            AuditAllowedActions = AuditActions.Create | AuditActions.Delete,
        };

        Assert.Equal(
            AuditActions.Create | AuditActions.Delete,
            settings.EffectiveActions(AuditCohort.AccountMember, platform)
        );
    }

    [Theory]
    [InlineData(AuditCohort.Accountless)]
    [InlineData(AuditCohort.SystemContext)]
    public void EffectiveActions_ReturnsNone_ForCohortsTheAccountDoesNotConfigure(
        AuditCohort cohort
    )
    {
        var settings = new AccountAuditSettings(_accountId, AuditActions.All, AuditActions.All, 30);

        Assert.Equal(
            AuditActions.None,
            settings.EffectiveActions(cohort, PlatformSettings.Default)
        );
    }

    [Theory]
    [InlineData(30, 365, 30)]
    [InlineData(500, 365, 365)]
    [InlineData(0, 365, 0)]
    [InlineData(-5, 365, 0)]
    public void EffectiveRetentionDays_StaysWithinThePlatformMaximum(
        int retentionDays,
        int platformMaximum,
        int expected
    )
    {
        var settings = new AccountAuditSettings(
            _accountId,
            AuditActions.None,
            AuditActions.None,
            retentionDays
        );
        var platform = PlatformSettings.Default with { AuditMaxRetentionDays = platformMaximum };

        Assert.Equal(expected, settings.EffectiveRetentionDays(platform));
    }
}
