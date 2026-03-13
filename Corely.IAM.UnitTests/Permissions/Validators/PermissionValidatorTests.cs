using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Providers;
using Corely.IAM.Permissions.Validators;
using Corely.IAM.UnitTests.ClassData;
using FluentValidation.TestHelper;
using Moq;

namespace Corely.IAM.UnitTests.Permissions.Validators;

public class PermissionValidatorTests
{
    private readonly Mock<IResourceTypeRegistry> _mockRegistry = new();
    private readonly PermissionValidator _validator;

    public PermissionValidatorTests()
    {
        _mockRegistry.Setup(r => r.Exists(It.IsAny<string>())).Returns(true);
        _validator = new PermissionValidator(_mockRegistry.Object);
    }

    [Theory, ClassData(typeof(NullEmptyAndWhitespace))]
    public void PermissionValidator_HasValidationError_WhenResourceTypeInvalid(string resourceType)
    {
        var permission = new Permission { ResourceType = resourceType, Create = true };

        var result = _validator.TestValidate(permission);
        result.ShouldHaveValidationErrorFor(x => x.ResourceType);
    }

    [Fact]
    public void PermissionValidator_HasValidationError_WhenResourceTypeNotRegistered()
    {
        _mockRegistry.Setup(r => r.Exists("unknown")).Returns(false);
        var permission = new Permission { ResourceType = "unknown", Create = true };

        var result = _validator.TestValidate(permission);
        result.ShouldHaveValidationErrorFor(x => x.ResourceType);
    }

    [Fact]
    public void PermissionValidator_HasNoValidationError_WhenResourceTypeRegistered()
    {
        var permission = new Permission
        {
            ResourceType = "group",
            ResourceId = Guid.Empty,
            Create = true,
        };

        var result = _validator.TestValidate(permission);
        result.ShouldNotHaveValidationErrorFor(x => x.ResourceType);
    }

    [Fact]
    public void PermissionValidator_HasValidationError_WhenNoCrudxFlagsSet()
    {
        var permission = new Permission { ResourceType = "group", ResourceId = Guid.Empty };

        var result = _validator.TestValidate(permission);
        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Theory]
    [InlineData(true, false, false, false, false)]
    [InlineData(false, true, false, false, false)]
    [InlineData(false, false, true, false, false)]
    [InlineData(false, false, false, true, false)]
    [InlineData(false, false, false, false, true)]
    [InlineData(true, true, true, true, true)]
    public void PermissionValidator_HasNoValidationError_WhenAtLeastOneCrudxFlagSet(
        bool create,
        bool read,
        bool update,
        bool delete,
        bool execute
    )
    {
        var permission = new Permission
        {
            ResourceType = "group",
            ResourceId = Guid.Empty,
            Create = create,
            Read = read,
            Update = update,
            Delete = delete,
            Execute = execute,
        };

        var result = _validator.TestValidate(permission);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
