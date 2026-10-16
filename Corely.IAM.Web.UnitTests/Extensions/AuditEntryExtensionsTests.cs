using Corely.IAM.Audits.Models;
using Corely.IAM.Security.Enums;
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

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(3, 3, 0)]
    [InlineData(5, 3, 2)]
    public void ShownAndHiddenResourceIds_SplitAtTheMaximum(int total, int shown, int hidden)
    {
        var entry = Entry() with
        {
            ResourceIds = [.. Enumerable.Range(0, total).Select(_ => Guid.CreateVersion7())],
        };

        Assert.Equal(entry.ResourceIds.Take(shown), entry.ShownResourceIds(3));
        Assert.Equal(hidden, entry.HiddenResourceIdCount(3));
    }

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
    public void AccountDisplayName_PrefersTheName_ThenTheId_ThenADash()
    {
        var accountId = Guid.CreateVersion7();

        Assert.Equal(
            "Acme",
            (Entry(accountId: accountId) with { AccountName = "Acme" }).AccountDisplayName()
        );
        Assert.Equal(accountId.ToString(), Entry(accountId: accountId).AccountDisplayName());
        Assert.Equal("—", Entry().AccountDisplayName());
    }

    [Theory]
    [InlineData("SignInAsync", "Sign in")]
    [InlineData("RegisterUsersWithGroupAsync", "Register users with group")]
    [InlineData("VerifyMfaAsync", "Verify MFA")]
    [InlineData("GetTotpStatusAsync", "Get TOTP status")]
    [InlineData("Async", "Async")]
    [InlineData("Approve", "Approve")]
    public void Description_IsTheOperationInPlainWords(string operation, string expected) =>
        Assert.Equal(expected, (Entry() with { Operation = operation }).Description());

    [Theory]
    [InlineData("Success", "Success", true)]
    [InlineData("InvalidPasswordError", "Invalid password", false)]
    [InlineData("UserLockedError", "User locked", false)]
    [InlineData("Error", "Error", false)]
    public void Result_IsPlainWords_AndSucceededOnlyOnSuccess(
        string resultCode,
        string expected,
        bool succeeded
    )
    {
        var entry = Entry() with { ResultCode = resultCode };

        Assert.Equal(expected, entry.ResultText());
        Assert.Equal(succeeded, entry.Succeeded());
    }
}
