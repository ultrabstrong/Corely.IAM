using Corely.IAM.Models;

namespace Corely.IAM.UnitTests.Models;

public class PagedResultTests
{
    [Fact]
    public void Create_WithFirstPage_ReturnsPage1()
    {
        var result = PagedResult<string>.Create(["a", "b"], totalCount: 10, skip: 0, take: 2);
        Assert.Equal(1, result.CurrentPage);
    }

    [Fact]
    public void Create_WithSkip_CalculatesCorrectPage()
    {
        var result = PagedResult<string>.Create(["c", "d"], totalCount: 10, skip: 4, take: 2);
        Assert.Equal(3, result.CurrentPage);
    }

    [Fact]
    public void Create_WhenMoreItemsExist_HasMoreIsTrue()
    {
        var result = PagedResult<string>.Create(["a", "b"], totalCount: 10, skip: 0, take: 2);
        Assert.True(result.HasMore);
    }

    [Fact]
    public void Create_WhenOnLastPage_HasMoreIsFalse()
    {
        var result = PagedResult<string>.Create(["i", "j"], totalCount: 10, skip: 8, take: 2);
        Assert.False(result.HasMore);
    }

    [Fact]
    public void Create_WhenExactlyAtEnd_HasMoreIsFalse()
    {
        var result = PagedResult<string>.Create(["e"], totalCount: 5, skip: 4, take: 5);
        Assert.False(result.HasMore);
    }

    [Fact]
    public void Create_PreservesItemsAndTotalCount()
    {
        var items = new List<string> { "a", "b", "c" };
        var result = PagedResult<string>.Create(items, totalCount: 50, skip: 0, take: 3);
        Assert.Equal(items, result.Items);
        Assert.Equal(50, result.TotalCount);
    }

    [Fact]
    public void Create_WithZeroTake_ReturnsPage1()
    {
        var result = PagedResult<string>.Create([], totalCount: 0, skip: 0, take: 0);
        Assert.Equal(1, result.CurrentPage);
    }

    [Fact]
    public void Empty_ReturnsEmptyResult()
    {
        var result = PagedResult<string>.Empty();
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.CurrentPage);
        Assert.False(result.HasMore);
    }
}
