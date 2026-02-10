using Corely.IAM.Filtering;
using Corely.IAM.Filtering.Filters;
using FluentAssertions;

namespace Corely.IAM.UnitTests.Filtering.Filters;

public class ComparableFilterTests
{
    private static readonly List<TestEntity> TestData =
    [
        new()
        {
            Name = "A",
            Age = 10,
            NullableAge = null,
            Price = 1.50m,
            CreatedDate = new DateTime(2024, 1, 1),
        },
        new()
        {
            Name = "B",
            Age = 20,
            NullableAge = 20,
            Price = 2.50m,
            CreatedDate = new DateTime(2024, 6, 15),
        },
        new()
        {
            Name = "C",
            Age = 30,
            NullableAge = 30,
            Price = 3.50m,
            CreatedDate = new DateTime(2024, 12, 31),
        },
    ];

    private List<TestEntity> ApplyFilter(FilterBuilder<TestEntity> builder)
    {
        var predicate = builder.Build();
        return predicate == null ? TestData : TestData.AsQueryable().Where(predicate).ToList();
    }

    [Fact]
    public void Equals_FiltersToExactValue()
    {
        var builder = Filter.For<TestEntity>().Where(e => e.Age, ComparableFilter<int>.Equals(20));
        var results = ApplyFilter(builder);
        results.Should().HaveCount(1).And.OnlyContain(e => e.Age == 20);
    }

    [Fact]
    public void NotEquals_ExcludesExactValue()
    {
        var builder = Filter
            .For<TestEntity>()
            .Where(e => e.Age, ComparableFilter<int>.NotEquals(20));
        var results = ApplyFilter(builder);
        results.Should().HaveCount(2).And.OnlyContain(e => e.Age != 20);
    }

    [Fact]
    public void GreaterThan_FiltersAboveThreshold()
    {
        var builder = Filter
            .For<TestEntity>()
            .Where(e => e.Age, ComparableFilter<int>.GreaterThan(15));
        var results = ApplyFilter(builder);
        results.Should().HaveCount(2).And.OnlyContain(e => e.Age > 15);
    }

    [Fact]
    public void GreaterThanOrEqual_FiltersAtOrAboveThreshold()
    {
        var builder = Filter
            .For<TestEntity>()
            .Where(e => e.Age, ComparableFilter<int>.GreaterThanOrEqual(20));
        var results = ApplyFilter(builder);
        results.Should().HaveCount(2).And.OnlyContain(e => e.Age >= 20);
    }

    [Fact]
    public void LessThan_FiltersBelowThreshold()
    {
        var builder = Filter
            .For<TestEntity>()
            .Where(e => e.Age, ComparableFilter<int>.LessThan(25));
        var results = ApplyFilter(builder);
        results.Should().HaveCount(2).And.OnlyContain(e => e.Age < 25);
    }

    [Fact]
    public void LessThanOrEqual_FiltersAtOrBelowThreshold()
    {
        var builder = Filter
            .For<TestEntity>()
            .Where(e => e.Age, ComparableFilter<int>.LessThanOrEqual(20));
        var results = ApplyFilter(builder);
        results.Should().HaveCount(2).And.OnlyContain(e => e.Age <= 20);
    }

    [Fact]
    public void Between_FiltersInclusiveRange()
    {
        var builder = Filter
            .For<TestEntity>()
            .Where(e => e.Age, ComparableFilter<int>.Between(10, 20));
        var results = ApplyFilter(builder);
        results.Should().HaveCount(2).And.OnlyContain(e => e.Age >= 10 && e.Age <= 20);
    }

    [Fact]
    public void NotBetween_FiltersOutsideRange()
    {
        var builder = Filter
            .For<TestEntity>()
            .Where(e => e.Age, ComparableFilter<int>.NotBetween(15, 25));
        var results = ApplyFilter(builder);
        results.Should().HaveCount(2).And.OnlyContain(e => e.Age < 15 || e.Age > 25);
    }

    [Fact]
    public void In_FiltersToValueSet()
    {
        var builder = Filter.For<TestEntity>().Where(e => e.Age, ComparableFilter<int>.In(10, 30));
        var results = ApplyFilter(builder);
        results.Should().HaveCount(2).And.OnlyContain(e => e.Age == 10 || e.Age == 30);
    }

    [Fact]
    public void NotIn_ExcludesValueSet()
    {
        var builder = Filter
            .For<TestEntity>()
            .Where(e => e.Age, ComparableFilter<int>.NotIn(10, 30));
        var results = ApplyFilter(builder);
        results.Should().HaveCount(1).And.OnlyContain(e => e.Age == 20);
    }

    [Fact]
    public void IsNull_FiltersNullableToNull()
    {
        var builder = Filter
            .For<TestEntity>()
            .Where(e => e.NullableAge, ComparableFilter<int>.IsNull());
        var results = ApplyFilter(builder);
        results.Should().HaveCount(1).And.OnlyContain(e => e.NullableAge == null);
    }

    [Fact]
    public void IsNotNull_FiltersNullableToNonNull()
    {
        var builder = Filter
            .For<TestEntity>()
            .Where(e => e.NullableAge, ComparableFilter<int>.IsNotNull());
        var results = ApplyFilter(builder);
        results.Should().HaveCount(2).And.OnlyContain(e => e.NullableAge != null);
    }

    [Fact]
    public void WorksWithDecimal()
    {
        var builder = Filter
            .For<TestEntity>()
            .Where(e => e.Price, ComparableFilter<decimal>.GreaterThan(2.00m));
        var results = ApplyFilter(builder);
        results.Should().HaveCount(2).And.OnlyContain(e => e.Price > 2.00m);
    }

    [Fact]
    public void WorksWithDateTime()
    {
        var builder = Filter
            .For<TestEntity>()
            .Where(
                e => e.CreatedDate,
                ComparableFilter<DateTime>.Between(
                    new DateTime(2024, 1, 1),
                    new DateTime(2024, 6, 30)
                )
            );
        var results = ApplyFilter(builder);
        results.Should().HaveCount(2);
    }
}
