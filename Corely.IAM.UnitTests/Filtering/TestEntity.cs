namespace Corely.IAM.UnitTests.Filtering;

public enum TestStatus
{
    Active,
    Inactive,
    Pending,
}

public class TestChild
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int Score { get; set; }
}

public class TestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int Age { get; set; }
    public int? NullableAge { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsActive { get; set; }
    public bool? IsVerified { get; set; }
    public TestStatus Status { get; set; }
    public TestStatus? NullableStatus { get; set; }
    public Guid AccountId { get; set; }
    public Guid? ParentId { get; set; }
    public List<TestChild> Children { get; set; } = [];
}
