using Corely.IAM.Filtering;
using Corely.IAM.Filtering.Filters;
using FluentAssertions;

namespace Corely.IAM.UnitTests.Filtering.Filters;

public class EnumFilterTests
{
    private static readonly List<TestEntity> TestData =
    [
        new()
        {
            Name = "A",
            Status = TestStatus.Active,
            NullableStatus = TestStatus.Active,
        },
        new()
        {
            Name = "B",
            Status = TestStatus.Inactive,
            NullableStatus = null,
        },
        new()
        {
            Name = "C",
            Status = TestStatus.Pending,
            NullableStatus = TestStatus.Pending,
        },
    ];

    private List<TestEntity> ApplyFilter(FilterBuilder<TestEntity> builder)
    {
        var predicate = builder.Build();
        return predicate == null ? TestData : TestData.AsQueryable().Where(predicate).ToList();
    }

    [Fact]
    public void Equals_FiltersToExactEnum()
    {
        var builder = Filter
            .For<TestEntity>()
            .Where(e => e.Status, EnumFilter<TestStatus>.Equals(TestStatus.Active));
        var results = ApplyFilter(builder);
        results.Should().HaveCount(1).And.OnlyContain(e => e.Status == TestStatus.Active);
    }

    [Fact]
    public void NotEquals_ExcludesExactEnum()
    {
        var builder = Filter
            .For<TestEntity>()
            .Where(e => e.Status, EnumFilter<TestStatus>.NotEquals(TestStatus.Active));
        var results = ApplyFilter(builder);
        results.Should().HaveCount(2).And.OnlyContain(e => e.Status != TestStatus.Active);
    }

    [Fact]
    public void In_FiltersToEnumSet()
    {
        var builder = Filter
            .For<TestEntity>()
            .Where(e => e.Status, EnumFilter<TestStatus>.In(TestStatus.Active, TestStatus.Pending));
        var results = ApplyFilter(builder);
        results
            .Should()
            .HaveCount(2)
            .And.OnlyContain(e => e.Status == TestStatus.Active || e.Status == TestStatus.Pending);
    }

    [Fact]
    public void NotIn_ExcludesEnumSet()
    {
        var builder = Filter
            .For<TestEntity>()
            .Where(
                e => e.Status,
                EnumFilter<TestStatus>.NotIn(TestStatus.Active, TestStatus.Pending)
            );
        var results = ApplyFilter(builder);
        results.Should().HaveCount(1).And.OnlyContain(e => e.Status == TestStatus.Inactive);
    }

    [Fact]
    public void IsNull_FiltersNullableEnumToNull()
    {
        var builder = Filter
            .For<TestEntity>()
            .Where(e => e.NullableStatus, EnumFilter<TestStatus>.IsNull());
        var results = ApplyFilter(builder);
        results.Should().HaveCount(1).And.OnlyContain(e => e.NullableStatus == null);
    }

    [Fact]
    public void IsNotNull_FiltersNullableEnumToNonNull()
    {
        var builder = Filter
            .For<TestEntity>()
            .Where(e => e.NullableStatus, EnumFilter<TestStatus>.IsNotNull());
        var results = ApplyFilter(builder);
        results.Should().HaveCount(2).And.OnlyContain(e => e.NullableStatus != null);
    }
}
