using Corely.IAM.Audits.Models;
using Corely.IAM.Web.Components.Shared;
using TestContext = Bunit.TestContext;

namespace Corely.IAM.Web.UnitTests.Components.Shared;

public class AuditActionRowTests : TestContext
{
    [Fact]
    public void Checkboxes_ShowTheCurrentActions()
    {
        var cut = RenderRow(p =>
            p.Add(x => x.IdPrefix, "test")
                .Add(x => x.Label, "Members")
                .Add(x => x.Value, AuditActions.Create | AuditActions.Delete)
        );

        Assert.True(cut.Find("#test-Create").HasAttribute("checked"));
        Assert.True(cut.Find("#test-Delete").HasAttribute("checked"));
        Assert.False(cut.Find("#test-Read").HasAttribute("checked"));
    }

    [Fact]
    public void Checkboxes_ShowAForbiddenActionUnchecked_EvenWhenTheAccountChoseIt()
    {
        var cut = RenderRow(p =>
            p.Add(x => x.IdPrefix, "test")
                .Add(x => x.Label, "Members")
                .Add(x => x.Value, AuditActions.Create | AuditActions.Read)
                .Add(x => x.Allowed, AuditActions.Create)
        );

        Assert.False(cut.Find("#test-Read").HasAttribute("checked"));
        Assert.True(cut.Find("#test-Create").HasAttribute("checked"));
    }

    [Fact]
    public void Checkboxes_DisableActionsThePlatformForbids()
    {
        var cut = RenderRow(p =>
            p.Add(x => x.IdPrefix, "test")
                .Add(x => x.Label, "Members")
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
    public void ReadOnly_ShowsEachActionEnabledDisabledOrNotAllowed()
    {
        var cut = RenderRow(p =>
            p.Add(x => x.IdPrefix, "test")
                .Add(x => x.Label, "Members")
                .Add(x => x.Value, AuditActions.Create | AuditActions.Read)
                .Add(x => x.Allowed, AuditActions.Create | AuditActions.Update)
                .Add(x => x.Editing, false)
        );

        Assert.Empty(cut.FindAll("input"));
        Assert.Equal("Members", cut.Find("tbody th").TextContent);
        Assert.Equal("C Enabled", cut.Find("#test-Create").TextContent);
        Assert.Contains("active", cut.Find("#test-Create").ClassList);
        Assert.Equal("U Disabled", cut.Find("#test-Update").TextContent);
        Assert.Contains("inactive", cut.Find("#test-Update").ClassList);
        Assert.Equal("R Not allowed", cut.Find("#test-Read").TextContent);
        Assert.Contains("not-allowed", cut.Find("#test-Read").ClassList);
    }

    private IRenderedComponent<AuditActionMatrix> RenderRow(
        Action<ComponentParameterCollectionBuilder<AuditActionRow>> row
    ) => Render<AuditActionMatrix>(p => p.AddChildContent(row));

    [Fact]
    public void Checking_ReportsTheNewActions()
    {
        AuditActions? changed = null;
        var cut = RenderRow(p =>
            p.Add(x => x.IdPrefix, "test")
                .Add(x => x.Label, "Members")
                .Add(x => x.Value, AuditActions.Create)
                .Add(x => x.ValueChanged, (AuditActions value) => changed = value)
        );

        cut.Find("#test-Read").Change(true);

        Assert.Equal(AuditActions.Create | AuditActions.Read, changed);
    }
}
