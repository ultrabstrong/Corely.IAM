using Corely.IAM.Audits.Models;
using Corely.IAM.Security.Constants;
using Corely.IAM.Web.Extensions;

namespace Corely.IAM.Web.UnitTests.Extensions;

public class AuditEntryExtensionsTests
{
    private static AuditEntry Entry(
        Guid? actorUserId = null,
        Guid? accountId = null,
        AuditCohort cohort = AuditCohort.AccountMember
    ) =>
        new(
            Guid.CreateVersion7(),
            DateTime.UtcNow,
            actorUserId,
            cohort,
            accountId,
            "tests",
            "IRegistrationService",
            "RegisterGroupAsync",
            AuthAction.Create,
            "group",
            [],
            "Success",
            null
        );

    [Fact]
    public void ActorDisplayName_PrefersTheName_ThenTheId()
    {
        var userId = Guid.CreateVersion7();

        Assert.Equal("alice", (Entry(userId) with { ActorName = "alice" }).ActorDisplayName());
        Assert.Equal(userId.ToString(), Entry(userId).ActorDisplayName());
    }

    [Theory]
    [InlineData(AuditCohort.SystemContext, "System")]
    [InlineData(AuditCohort.Accountless, "Unknown user")]
    public void ActorDisplayName_SaysWhoActed_WithoutAnActor(AuditCohort cohort, string expected) =>
        Assert.Equal(expected, Entry(cohort: cohort).ActorDisplayName());

    [Fact]
    public void AccountDisplayName_PrefersTheName_ThenTheId_ThenNoAccount()
    {
        var accountId = Guid.CreateVersion7();

        Assert.Equal(
            "Acme",
            (Entry(accountId: accountId) with { AccountName = "Acme" }).AccountDisplayName()
        );
        Assert.Equal(accountId.ToString(), Entry(accountId: accountId).AccountDisplayName());
        Assert.Equal("Outside any account", Entry().AccountDisplayName());
    }
}
