using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Providers;

namespace Corely.IAM.UnitTests.Permissions.Providers;

public class ResourceTypeRegistryTests
{
    private readonly ResourceTypeRegistry _registry = new();

    [Theory]
    [InlineData(PermissionConstants.ACCOUNT_RESOURCE_TYPE, "Accounts")]
    [InlineData(PermissionConstants.USER_RESOURCE_TYPE, "Users")]
    [InlineData(PermissionConstants.GROUP_RESOURCE_TYPE, "Groups")]
    [InlineData(PermissionConstants.ROLE_RESOURCE_TYPE, "Roles")]
    [InlineData(PermissionConstants.PERMISSION_RESOURCE_TYPE, "Permissions")]
    [InlineData(PermissionConstants.ALL_RESOURCE_TYPES, "All resource types (wildcard)")]
    public void Constructor_PreRegistersIAMType(string name, string expectedDescription)
    {
        var info = _registry.Get(name);

        Assert.NotNull(info);
        Assert.Equal(name, info.Name);
        Assert.Equal(expectedDescription, info.Description);
    }

    [Fact]
    public void GetAll_ReturnsAllRegisteredTypes()
    {
        var all = _registry.GetAll();

        Assert.Equal(6, all.Count);
    }

    [Fact]
    public void Get_ReturnsCorrectType()
    {
        var info = _registry.Get(PermissionConstants.ACCOUNT_RESOURCE_TYPE);

        Assert.NotNull(info);
        Assert.Equal(PermissionConstants.ACCOUNT_RESOURCE_TYPE, info.Name);
        Assert.Equal("Accounts", info.Description);
    }

    [Fact]
    public void Get_IsCaseInsensitive()
    {
        var info = _registry.Get("ACCOUNT");

        Assert.NotNull(info);
        Assert.Equal(PermissionConstants.ACCOUNT_RESOURCE_TYPE, info.Name);
    }

    [Fact]
    public void Get_WithUnknownName_ReturnsNull()
    {
        var info = _registry.Get("nonexistent");

        Assert.Null(info);
    }

    [Fact]
    public void Exists_WithKnownType_ReturnsTrue()
    {
        Assert.True(_registry.Exists(PermissionConstants.USER_RESOURCE_TYPE));
    }

    [Fact]
    public void Exists_WithUnknownType_ReturnsFalse()
    {
        Assert.False(_registry.Exists("nonexistent"));
    }

    [Fact]
    public void Register_AddsNewType()
    {
        _registry.Register("invoice", "Customer invoices");

        var info = _registry.Get("invoice");
        Assert.NotNull(info);
        Assert.Equal("invoice", info.Name);
        Assert.Equal("Customer invoices", info.Description);
    }

    [Fact]
    public void Register_WithDuplicateName_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _registry.Register(PermissionConstants.ACCOUNT_RESOURCE_TYPE, "Duplicate")
        );
    }

    [Theory]
    [InlineData("Account")]
    [InlineData("ACCOUNT")]
    [InlineData("aCCOUNT")]
    public void Register_WithCaseVariantDuplicate_ThrowsInvalidOperationException(string name)
    {
        Assert.Throws<InvalidOperationException>(() => _registry.Register(name, "Duplicate"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithNullOrWhitespaceName_ThrowsArgumentException(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() => _registry.Register(name!, "Description"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithNullOrWhitespaceDescription_ThrowsArgumentException(
        string? description
    )
    {
        Assert.ThrowsAny<ArgumentException>(() => _registry.Register("custom", description!));
    }
}
