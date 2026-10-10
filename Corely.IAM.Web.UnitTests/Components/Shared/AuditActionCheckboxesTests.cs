using Corely.IAM.Audits.Models;
using Corely.IAM.Web.Components.Shared;
using TestContext = Bunit.TestContext;

namespace Corely.IAM.Web.UnitTests.Components.Shared;

public class AuditActionCheckboxesTests : TestContext
{
    [Fact]
    public void Checkboxes_ShowTheCurrentActions()
    {
        var cut = Render<AuditActionCheckboxes>(p =>
            p.Add(x => x.IdPrefix, "test")
                .Add(x => x.Value, AuditActions.Create | AuditActions.Delete)
        );

        Assert.True(cut.Find("#test-Create").HasAttribute("checked"));
        Assert.True(cut.Find("#test-Delete").HasAttribute("checked"));
        Assert.False(cut.Find("#test-Read").HasAttribute("checked"));
    }

    [Fact]
    public void Checkboxes_ShowAForbiddenActionUnchecked_EvenWhenTheAccountChoseIt()
    {
        var cut = Render<AuditActionCheckboxes>(p =>
            p.Add(x => x.IdPrefix, "test")
                .Add(x => x.Value, AuditActions.Create | AuditActions.Read)
                .Add(x => x.Allowed, AuditActions.Create)
        );

        Assert.False(cut.Find("#test-Read").HasAttribute("checked"));
        Assert.True(cut.Find("#test-Create").HasAttribute("checked"));
    }

    [Fact]
    public void Checkboxes_DisableActionsThePlatformForbids()
    {
        var cut = Render<AuditActionCheckboxes>(p =>
            p.Add(x => x.IdPrefix, "test")
                .Add(
                    x => x.Allowed,
                    AuditActions.Create | AuditActions.Update | AuditActions.Delete
                )
        );

        Assert.True(cut.Find("#test-Read").HasAttribute("disabled"));
        Assert.True(cut.Find("#test-Execute").HasAttribute("disabled"));
        Assert.False(cut.Find("#test-Create").HasAttribute("disabled"));
    }

    [Fact]
    public void Checking_ReportsTheNewActions()
    {
        AuditActions? changed = null;
        var cut = Render<AuditActionCheckboxes>(p =>
            p.Add(x => x.IdPrefix, "test")
                .Add(x => x.Value, AuditActions.Create)
                .Add(x => x.ValueChanged, (AuditActions value) => changed = value)
        );

        cut.Find("#test-Read").Change(true);

        Assert.Equal(AuditActions.Create | AuditActions.Read, changed);
    }
}
