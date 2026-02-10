using Corely.IAM.Filtering;
using Corely.IAM.Filtering.Filters;
using FluentAssertions;

namespace Corely.IAM.UnitTests.Filtering.Filters;

public class BoolFilterTests
{
    private static readonly List<TestEntity> TestData =
    [
        new()
        {
            Name = "A",
            IsActive = true,
            IsVerified = true,
        },
        new()
        {
            Name = "B",
            IsActive = false,
            IsVerified = null,
        },
        new()
        {
            Name = "C",
            IsActive = true,
            IsVerified = false,
        },
    ];

    private List<TestEntity> ApplyFilter(FilterBuilder<TestEntity> builder)
    {
        var predicate = builder.Build();
        return predicate == null ? TestData : TestData.AsQueryable().Where(predicate).ToList();
    }

    [Fact]
    public void IsTrue_FiltersToTrueValues()
    {
        var builder = Filter.For<TestEntity>().Where(e => e.IsActive, BoolFilter.IsTrue());
        var results = ApplyFilter(builder);
        results.Should().HaveCount(2).And.OnlyContain(e => e.IsActive);
    }

    [Fact]
    public void IsFalse_FiltersToFalseValues()
    {
        var builder = Filter.For<TestEntity>().Where(e => e.IsActive, BoolFilter.IsFalse());
        var results = ApplyFilter(builder);
        results.Should().HaveCount(1).And.OnlyContain(e => !e.IsActive);
    }

    [Fact]
    public void IsNull_FiltersNullableBoolToNull()
    {
        var builder = Filter.For<TestEntity>().Where(e => e.IsVerified, BoolFilter.IsNull());
        var results = ApplyFilter(builder);
        results.Should().HaveCount(1).And.OnlyContain(e => e.IsVerified == null);
    }

    [Fact]
    public void IsNotNull_FiltersNullableBoolToNonNull()
    {
        var builder = Filter.For<TestEntity>().Where(e => e.IsVerified, BoolFilter.IsNotNull());
        var results = ApplyFilter(builder);
        results.Should().HaveCount(2).And.OnlyContain(e => e.IsVerified != null);
    }
}
