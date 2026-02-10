using Corely.IAM.Filtering.Ordering;
using FluentAssertions;

namespace Corely.IAM.UnitTests.Filtering.Ordering;

public class OrderBuilderTests
{
    private static readonly List<TestEntity> TestData =
    [
        new()
        {
            Name = "Charlie",
            Age = 30,
            CreatedDate = new DateTime(2024, 3, 1),
        },
        new()
        {
            Name = "Alice",
            Age = 20,
            CreatedDate = new DateTime(2024, 1, 1),
        },
        new()
        {
            Name = "Bob",
            Age = 20,
            CreatedDate = new DateTime(2024, 2, 1),
        },
    ];

    [Fact]
    public void By_Ascending_SortsByPropertyAscending()
    {
        var order = Order.For<TestEntity>().By(e => e.Name);
        var results = order.Apply(TestData.AsQueryable()).ToList();
        results.Select(e => e.Name).Should().ContainInOrder("Alice", "Bob", "Charlie");
    }

    [Fact]
    public void By_Descending_SortsByPropertyDescending()
    {
        var order = Order.For<TestEntity>().By(e => e.Name, SortDirection.Descending);
        var results = order.Apply(TestData.AsQueryable()).ToList();
        results.Select(e => e.Name).Should().ContainInOrder("Charlie", "Bob", "Alice");
    }

    [Fact]
    public void ThenBy_AppliesSecondarySort()
    {
        var order = Order.For<TestEntity>().By(e => e.Age).ThenBy(e => e.Name);
        var results = order.Apply(TestData.AsQueryable()).ToList();
        // Age 20: Alice, Bob (alphabetical); Age 30: Charlie
        results.Select(e => e.Name).Should().ContainInOrder("Alice", "Bob", "Charlie");
    }

    [Fact]
    public void ThenBy_Descending_AppliesSecondarySortDescending()
    {
        var order = Order
            .For<TestEntity>()
            .By(e => e.Age)
            .ThenBy(e => e.Name, SortDirection.Descending);
        var results = order.Apply(TestData.AsQueryable()).ToList();
        // Age 20: Bob, Alice (reverse alphabetical); Age 30: Charlie
        results.Select(e => e.Name).Should().ContainInOrder("Bob", "Alice", "Charlie");
    }

    [Fact]
    public void MultipleThenBy_AppliesTertiarySort()
    {
        var order = Order
            .For<TestEntity>()
            .By(e => e.Age)
            .ThenBy(e => e.CreatedDate)
            .ThenBy(e => e.Name);
        var results = order.Apply(TestData.AsQueryable()).ToList();
        // Age 20: Alice (Jan), Bob (Feb); Age 30: Charlie (Mar)
        results.Select(e => e.Name).Should().ContainInOrder("Alice", "Bob", "Charlie");
    }

    [Fact]
    public void By_CalledTwice_ResetsToNewSort()
    {
        var order = Order.For<TestEntity>().By(e => e.Age).By(e => e.Name);
        var results = order.Apply(TestData.AsQueryable()).ToList();
        // Second By() should have cleared the Age sort
        results.Select(e => e.Name).Should().ContainInOrder("Alice", "Bob", "Charlie");
    }

    [Fact]
    public void ThenBy_WithoutBy_ThrowsInvalidOperation()
    {
        var act = () => Order.For<TestEntity>().ThenBy(e => e.Name);
        act.Should().Throw<InvalidOperationException>().WithMessage("*after By*");
    }

    [Fact]
    public void Apply_WithNoClauses_ThrowsInvalidOperation()
    {
        var order = Order.For<TestEntity>();
        var act = () => order.Apply(TestData.AsQueryable());
        act.Should().Throw<InvalidOperationException>().WithMessage("*No ordering*");
    }

    [Fact]
    public void Build_ReturnsClauseList()
    {
        var order = Order
            .For<TestEntity>()
            .By(e => e.Name, SortDirection.Ascending)
            .ThenBy(e => e.Age, SortDirection.Descending);
        var clauses = order.Build();
        clauses.Should().HaveCount(2);
        clauses[0].Direction.Should().Be(SortDirection.Ascending);
        clauses[0].IsPrimary.Should().BeTrue();
        clauses[1].Direction.Should().Be(SortDirection.Descending);
        clauses[1].IsPrimary.Should().BeFalse();
    }
}
