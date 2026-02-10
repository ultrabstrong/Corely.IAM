using Corely.IAM.Models;
using FluentAssertions;

namespace Corely.IAM.UnitTests.Models;

public class PagedResultTests
{
    [Fact]
    public void Create_WithFirstPage_ReturnsPage1()
    {
        var result = PagedResult<string>.Create(["a", "b"], totalCount: 10, skip: 0, take: 2);
        result.CurrentPage.Should().Be(1);
    }

    [Fact]
    public void Create_WithSkip_CalculatesCorrectPage()
    {
        var result = PagedResult<string>.Create(["c", "d"], totalCount: 10, skip: 4, take: 2);
        result.CurrentPage.Should().Be(3);
    }

    [Fact]
    public void Create_WhenMoreItemsExist_HasMoreIsTrue()
    {
        var result = PagedResult<string>.Create(["a", "b"], totalCount: 10, skip: 0, take: 2);
        result.HasMore.Should().BeTrue();
    }

    [Fact]
    public void Create_WhenOnLastPage_HasMoreIsFalse()
    {
        var result = PagedResult<string>.Create(["i", "j"], totalCount: 10, skip: 8, take: 2);
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public void Create_WhenExactlyAtEnd_HasMoreIsFalse()
    {
        var result = PagedResult<string>.Create(["e"], totalCount: 5, skip: 4, take: 5);
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public void Create_PreservesItemsAndTotalCount()
    {
        var items = new List<string> { "a", "b", "c" };
        var result = PagedResult<string>.Create(items, totalCount: 50, skip: 0, take: 3);
        result.Items.Should().BeEquivalentTo(items);
        result.TotalCount.Should().Be(50);
    }

    [Fact]
    public void Create_WithZeroTake_ReturnsPage1()
    {
        var result = PagedResult<string>.Create([], totalCount: 0, skip: 0, take: 0);
        result.CurrentPage.Should().Be(1);
    }

    [Fact]
    public void Empty_ReturnsEmptyResult()
    {
        var result = PagedResult<string>.Empty();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.CurrentPage.Should().Be(1);
        result.HasMore.Should().BeFalse();
    }
}
