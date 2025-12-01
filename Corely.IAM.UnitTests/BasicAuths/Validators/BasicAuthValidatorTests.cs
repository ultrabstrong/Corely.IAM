using Corely.IAM.BasicAuths.Constants;
using Corely.IAM.BasicAuths.Validators;
using Corely.IAM.UnitTests.ClassData;
using Corely.Security.Hashing.Models;
using Corely.Security.Hashing.Providers;
using FluentValidation.TestHelper;

namespace Corely.IAM.UnitTests.BasicAuths.Validators;

public class BasicAuthValidatorTests
{
    private readonly BasicAuthValidator _validator = new();

    [Theory]
    [ClassData(typeof(NullEmptyAndWhitespace))]
    [MemberData(nameof(InvalidPasswordData))]
    public void BasicAuthValidator_HasValidationError_WhenPasswordInvalid(string password)
    {
        var basicAuth = new Corely.IAM.BasicAuths.Models.BasicAuth
        {
            Password = new HashedValue(Mock.Of<IHashProvider>()) { Hash = password },
        };

        var result = _validator.TestValidate(basicAuth);
        result.ShouldHaveValidationErrorFor(x => x.Password.Hash);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    public static IEnumerable<object[]> InvalidPasswordData() =>
        [
            [new string('a', BasicAuthConstants.PASSWORD_MAX_LENGTH + 1)],
        ];

    [Fact]
    public void BasicAuthValidator_HasValidationError_WhenPasswordIsNull()
    {
        var basicAuth = new Corely.IAM.BasicAuths.Models.BasicAuth { Password = null };

        var result = _validator.TestValidate(basicAuth);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
