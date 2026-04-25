using Corely.IAM.TotpAuths.Constants;
using Corely.IAM.TotpAuths.Models;
using FluentValidation;

namespace Corely.IAM.TotpAuths.Validators;

internal class ConfirmTotpRequestValidator : AbstractValidator<ConfirmTotpRequest>
{
    public ConfirmTotpRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(TotpAuthConstants.TOTP_CODE_LENGTH);
    }
}
