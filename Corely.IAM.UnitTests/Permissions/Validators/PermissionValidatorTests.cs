using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Validators;
using Corely.IAM.UnitTests.ClassData;
using FluentValidation.TestHelper;

namespace Corely.IAM.UnitTests.Permissions.Validators;

public class PermissionValidatorTests
{
    private readonly PermissionValidator _validator = new();

    [Theory, ClassData(typeof(NullEmptyAndWhitespace))]
    public void PermissionValidator_HasValidationError_WhenPermissionNameInvalid(
        string permissionName
    )
    {
        var permission = new Permission { Name = permissionName };

        var result = _validator.TestValidate(permission);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory, MemberData(nameof(InvalidPermissionTestData))]
    public void PermissionValidator_HasValidationError_WhenPermissionNameLengthInvalid(
        string permissionName
    )
    {
        var permission = new Permission { Name = permissionName };
        var result = _validator.TestValidate(permission);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    public static IEnumerable<object[]> InvalidPermissionTestData =>
        [
            [new string('a', PermissionConstants.PERMISSION_NAME_MIN_LENGTH - 1)],
            [new string('a', PermissionConstants.PERMISSION_NAME_MAX_LENGTH + 1)],
        ];
}
