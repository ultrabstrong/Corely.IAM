using Corely.IAM.UnitTests.Mappers.AutoMapper;
using CorelyValidationResult = Corely.IAM.Validators.ValidationResult;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace Corely.IAM.UnitTests.Validators.Mappers;

public class ValidationResultProfileTests
{
    public class ToCorelyValidationResult
        : ProfileTestsBase<FluentValidationResult, CorelyValidationResult> { }
}
