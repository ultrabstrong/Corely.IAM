using Corely.IAM.Audits.Entities;
using Corely.IAM.Audits.Providers;

namespace Corely.IAM.UnitTests.Audits.Providers;

public class AuditAccessTests
{
    private readonly Guid _accountId = Guid.CreateVersion7();
    private readonly Guid _viewerId = Guid.CreateVersion7();

    [Fact]
    public void CanReach_AnyAccountOrNone_WithEverything()
    {
        Assert.True(AuditAccess.All.CanReach(_accountId));
        Assert.True(AuditAccess.All.CanReach(null));
    }

    [Fact]
    public void CanReach_OnlyTheListedAccounts_Otherwise()
    {
        var access = new AuditAccess(false, new HashSet<Guid> { _accountId }, _viewerId);

        Assert.True(access.CanReach(_accountId));
        Assert.False(access.CanReach(Guid.CreateVersion7()));
        Assert.False(access.CanReach(null));
    }

    [Fact]
    public void VisibleEntries_AreTheListedAccountsAndTheViewersOwn()
    {
        var visible = new AuditAccess(false, new HashSet<Guid> { _accountId }, _viewerId)
            .VisibleEntries()
            .Compile();

        Assert.True(visible(new AuditEntryEntity { AccountId = _accountId }));
        Assert.True(visible(new AuditEntryEntity { ActorUserId = _viewerId }));
        Assert.False(visible(new AuditEntryEntity { AccountId = Guid.CreateVersion7() }));
        Assert.False(visible(new AuditEntryEntity()));
    }

    [Fact]
    public void VisibleEntries_IsNothing_ForNoAccess()
    {
        var visible = AuditAccess.None.VisibleEntries().Compile();

        Assert.False(visible(new AuditEntryEntity { AccountId = _accountId }));
        Assert.False(visible(new AuditEntryEntity()));
    }

    [Fact]
    public void VisibleEntries_IsEverything_WithEverything()
    {
        Assert.True(AuditAccess.All.VisibleEntries().Compile()(new AuditEntryEntity()));
    }
}
