using Corely.IAM.Accounts.Constants;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Validators;
using Corely.IAM.UnitTests.ClassData;
using FluentValidation.TestHelper;

namespace Corely.IAM.UnitTests.Accounts.Validators;

public class AccountValidatorTests
{
    private readonly AccountValidator _validator = new();

    [Theory]
    [ClassData(typeof(NullEmptyAndWhitespace))]
    [MemberData(nameof(InvalidAccountTestData))]
    public void AccountValidator_HasValidationError_WhenAccountNameInvalid(string accountName)
    {
        var account = new Account { AccountName = accountName };

        var result = _validator.TestValidate(account);
        result.ShouldHaveValidationErrorFor(x => x.AccountName);
    }

    public static IEnumerable<object[]> InvalidAccountTestData =>
        [
            [new string('a', AccountConstants.ACCOUNT_NAME_MIN_LENGTH - 1)],
            [new string('a', AccountConstants.ACCOUNT_NAME_MAX_LENGTH + 1)],
        ];
}
