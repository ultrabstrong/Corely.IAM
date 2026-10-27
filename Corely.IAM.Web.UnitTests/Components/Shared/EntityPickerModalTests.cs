using Corely.IAM.Web.Components.Shared;

namespace Corely.IAM.Web.UnitTests.Components.Shared;

public class EntityPickerModalTests : BunitContext
{
    private async Task<IRenderedComponent<EntityPickerModal>> OpenAsync(List<PickerItem> items)
    {
        var cut = Render<EntityPickerModal>(p =>
            p.Add(x => x.FetchItemsAsync, (int _, int _) => Task.FromResult((items, items.Count)))
        );
        await cut.InvokeAsync(() => cut.Instance.ShowAsync());
        return cut;
    }

    [Fact]
    public async Task Show_HeadsItemsByGroupInGroupThenNameOrder_ForGroupedItems()
    {
        var cut = await OpenAsync([
            new(Guid.CreateVersion7(), "Read grants", null, "Grants"),
            new(Guid.CreateVersion7(), "Manage users", null, "Users"),
            new(Guid.CreateVersion7(), "All grants", null, "Grants"),
        ]);

        cut.WaitForAssertion(() =>
            Assert.Equal(
                ["Grants", "Users"],
                cut.FindAll(".picker-group-heading").Select(e => e.TextContent)
            )
        );
        Assert.Equal(
            ["All grants", "Read grants", "Manage users"],
            cut.FindAll(".picker-item-text").Select(e => e.TextContent)
        );
    }

    [Fact]
    public async Task Show_KeepsTheFetchedOrderWithoutHeadings_ForUngroupedItems()
    {
        var cut = await OpenAsync([
            new(Guid.CreateVersion7(), "Zed"),
            new(Guid.CreateVersion7(), "Amy"),
        ]);

        cut.WaitForAssertion(() =>
            Assert.Equal(
                ["Zed", "Amy"],
                cut.FindAll(".picker-item-text").Select(e => e.TextContent)
            )
        );
        Assert.Empty(cut.FindAll(".picker-group-heading"));
    }

    [Fact]
    public async Task Show_OrdersGroupsByTheirOrderBeforeTheirName_ForOrderedGroups()
    {
        var cut = await OpenAsync([
            new(Guid.CreateVersion7(), "Zed", null, "Accounts", 2),
            new(Guid.CreateVersion7(), "Amy", null, "Users", 1),
        ]);

        cut.WaitForAssertion(() =>
            Assert.Equal(
                ["Users", "Accounts"],
                cut.FindAll(".picker-group-heading").Select(e => e.TextContent)
            )
        );
    }
}
