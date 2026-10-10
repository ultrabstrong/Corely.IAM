using Corely.IAM.Audits.Constants;
using Corely.IAM.Audits.Models;
using Corely.IAM.Platform.Models;

namespace Corely.IAM.UnitTests.Platform.Models;

public class PlatformSettingsTests
{
    private static readonly AccountAuditSettings _account = new(
        Guid.CreateVersion7(),
        AuditActions.Create,
        AuditActions.Delete,
        30
    );

    [Fact]
    public void Default_EnablesAuditing_WithSignInsRecordedAndSystemContextOff()
    {
        var settings = PlatformSettings.Default;

        Assert.True(settings.AuditEnabled);
        Assert.Equal(AuditConstants.DEFAULT_MAX_RETENTION_DAYS, settings.AuditMaxRetentionDays);
        Assert.Equal(AuditActions.All, settings.AuditAllowedActions);
        Assert.Equal(AuditActions.None, settings.SystemContextActions);
        Assert.Equal(
            AuditActions.Create | AuditActions.Update | AuditActions.Delete | AuditActions.Execute,
            settings.AccountlessActions
        );
    }

    [Theory]
    [InlineData(AuditCohort.AccountMember, AuditActions.Create)]
    [InlineData(AuditCohort.PlatformMember, AuditActions.Delete)]
    [InlineData(AuditCohort.SystemContext, AuditActions.Read)]
    [InlineData(AuditCohort.Accountless, AuditActions.Execute)]
    public void RecordedActions_UsesTheSettingThatOwnsTheCohort(
        AuditCohort cohort,
        AuditActions expected
    )
    {
        var settings = PlatformSettings.Default with
        {
            SystemContextActions = AuditActions.Read,
            AccountlessActions = AuditActions.Execute,
        };

        Assert.Equal(expected, settings.RecordedActions(cohort, _account));
    }

    [Theory]
    [InlineData(AuditCohort.AccountMember)]
    [InlineData(AuditCohort.PlatformMember)]
    [InlineData(AuditCohort.SystemContext)]
    [InlineData(AuditCohort.Accountless)]
    public void RecordedActions_ReturnsNone_WhenAuditingIsSwitchedOff(AuditCohort cohort)
    {
        var settings = PlatformSettings.Default with
        {
            AuditEnabled = false,
            SystemContextActions = AuditActions.All,
        };

        Assert.Equal(AuditActions.None, settings.RecordedActions(cohort, _account));
    }

    [Fact]
    public void RecordedActions_ReturnsNone_ForAccountCohortWithoutAccountSettings()
    {
        Assert.Equal(
            AuditActions.None,
            PlatformSettings.Default.RecordedActions(AuditCohort.AccountMember, null)
        );
    }
}
