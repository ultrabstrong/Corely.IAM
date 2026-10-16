using Corely.IAM.TotpAuths.Models;
using FluentValidation;

namespace Corely.IAM.TotpAuths.Validators;

internal class ConfirmTotpRequestValidator : AbstractValidator<ConfirmTotpRequest>
{
    public ConfirmTotpRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(ConfirmTotpRequest.CODE_LENGTH);
    }
}
