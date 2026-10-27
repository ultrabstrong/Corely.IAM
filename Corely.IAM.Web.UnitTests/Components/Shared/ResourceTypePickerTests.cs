using Corely.IAM.Permissions.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Web.Components.Shared;

namespace Corely.IAM.Web.UnitTests.Components.Shared;

public class ResourceTypePickerTests : BunitContext
{
    private static ResourceTypeInfo Type(string name, string title, string group) =>
        new(name, $"About {title}", Array.Empty<AuthAction>()) { Title = title, Group = group };

    private static readonly ResourceTypeInfo[] _types =
    [
        Type("quota", "Quota", "Billing"),
        Type("user", "Users", ResourceTypeInfo.IDENTITY_AND_ACCESS_GROUP),
        Type("grant", "Grants", "Billing"),
    ];

    [Fact]
    public void Render_HeadsEachGroupAndShowsTitleNameAndDescription_ForEditableTypes()
    {
        var cut = Render<ResourceTypePicker>(p =>
            p.Add(x => x.Types, _types).Add(x => x.IdPrefix, "t")
        );

        Assert.Equal(
            [ResourceTypeInfo.IDENTITY_AND_ACCESS_GROUP, "Billing"],
            cut.FindAll(".type-picker-group").Select(e => e.TextContent)
        );
        Assert.Equal(
            ["Users", "Grants", "Quota"],
            cut.FindAll(".type-picker-title").Select(e => e.TextContent)
        );
        Assert.Contains("About Grants", cut.Find("label[for='t-grant']").TextContent);
        Assert.Equal("grant", cut.Find("label[for='t-grant'] .type-picker-name").TextContent);
    }

    [Fact]
    public void Change_ReportsTheChosenType_ForSingleChoice()
    {
        string? chosen = null;
        var cut = Render<ResourceTypePicker>(p =>
            p.Add(x => x.Types, _types)
                .Add(x => x.IdPrefix, "t")
                .Add(x => x.ValueChanged, (string? v) => chosen = v)
        );

        cut.Find("#t-quota").Change(true);

        Assert.Equal("quota", chosen);
        Assert.Equal("radio", cut.Find("#t-quota").GetAttribute("type"));
    }

    [Fact]
    public void Change_AddsAndRemovesTypes_ForSeveralChoices()
    {
        IReadOnlySet<string> selected = new HashSet<string> { "user" };
        var cut = Render<ResourceTypePicker>(p =>
            p.Add(x => x.Types, _types)
                .Add(x => x.IdPrefix, "t")
                .Add(x => x.Multiple, true)
                .Add(x => x.Selected, selected)
                .Add(x => x.SelectedChanged, (IReadOnlySet<string> s) => selected = s)
        );

        cut.Find("#t-grant").Change(true);
        Assert.Equal(["grant", "user"], selected.Order());

        cut.Render(p => p.Add(x => x.Selected, selected));
        cut.Find("#t-user").Change(false);
        Assert.Equal(["grant"], selected);
    }

    [Fact]
    public void Render_ShowsOnlyTheChosenTypesWithoutInputs_ForReadOnly()
    {
        var cut = Render<ResourceTypePicker>(p =>
            p.Add(x => x.Types, _types)
                .Add(x => x.IdPrefix, "t")
                .Add(x => x.Multiple, true)
                .Add(x => x.ReadOnly, true)
                .Add(x => x.Selected, new HashSet<string> { "quota" })
        );

        Assert.Empty(cut.FindAll("input"));
        Assert.Equal(["Quota"], cut.FindAll(".type-picker-title").Select(e => e.TextContent));
    }

    [Fact]
    public void Render_SaysNone_ForReadOnlyWithNothingChosen()
    {
        var cut = Render<ResourceTypePicker>(p =>
            p.Add(x => x.Types, _types).Add(x => x.IdPrefix, "t").Add(x => x.ReadOnly, true)
        );

        Assert.Contains("None", cut.Markup);
    }

    [Fact]
    public void Render_LaysTheTypesOutAsCards_ForTheCardLayout()
    {
        var cut = Render<ResourceTypePicker>(p =>
            p.Add(x => x.Types, _types).Add(x => x.IdPrefix, "t").Add(x => x.Cards, true)
        );

        Assert.Equal(2, cut.FindAll(".type-picker-cards").Count);
        Assert.Empty(cut.FindAll(".type-picker-list"));
    }

    [Fact]
    public void Input_NarrowsTheTypes_ForASearchOnALongList()
    {
        var many = Enumerable
            .Range(1, 9)
            .Select(i => Type($"type_{i}", $"Type {i}", "Host"))
            .ToArray();
        var cut = Render<ResourceTypePicker>(p =>
            p.Add(x => x.Types, many).Add(x => x.IdPrefix, "t")
        );

        cut.Find("input[type='search']").Input("Type 7");

        Assert.Equal(["Type 7"], cut.FindAll(".type-picker-title").Select(e => e.TextContent));
    }
}
