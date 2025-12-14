using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Validators;
using Corely.IAM.UnitTests.ClassData;
using FluentValidation.TestHelper;

namespace Corely.IAM.UnitTests.Roles.Validators;

public class RoleValidatorTests
{
    private readonly RoleValidator _validator = new();

    [Theory]
    [ClassData(typeof(NullEmptyAndWhitespace))]
    public void RoleValidator_HasValidationError_WhenRoleNameInvalid(string roleName)
    {
        var role = new Role { Name = roleName };
        var result = _validator.TestValidate(role);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory, MemberData(nameof(InvalidRoleTestData))]
    public void RoleValidator_HasValidationError_WhenRoleNameLengthInvalid(string roleName)
    {
        var role = new Role { Name = roleName };
        var result = _validator.TestValidate(role);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    public static IEnumerable<object[]> InvalidRoleTestData =>
        [
            [new string('a', RoleConstants.ROLE_NAME_MIN_LENGTH - 1)],
            [new string('a', RoleConstants.ROLE_NAME_MAX_LENGTH + 1)],
        ];
}
