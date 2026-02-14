using Corely.IAM.Accounts.Constants;
using Corely.IAM.Accounts.Models;
using FluentValidation;

namespace Corely.IAM.Accounts.Validators;

internal class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    public UpdateAccountRequestValidator()
    {
        RuleFor(x => x.AccountName)
            .NotEmpty()
            .MinimumLength(AccountConstants.ACCOUNT_NAME_MIN_LENGTH)
            .MaximumLength(AccountConstants.ACCOUNT_NAME_MAX_LENGTH);
    }
}
