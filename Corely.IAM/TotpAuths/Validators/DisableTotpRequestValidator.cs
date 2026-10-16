using Corely.IAM.TotpAuths.Models;
using FluentValidation;

namespace Corely.IAM.TotpAuths.Validators;

internal class DisableTotpRequestValidator : AbstractValidator<DisableTotpRequest>
{
    public DisableTotpRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(DisableTotpRequest.CODE_LENGTH);
    }
}
