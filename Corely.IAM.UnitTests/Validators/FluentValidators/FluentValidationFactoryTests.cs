using Corely.IAM.Validators.FluentValidators;
using FluentValidation;

namespace Corely.IAM.UnitTests.Validators.FluentValidators;

public class FluentValidationFactoryTests
{
    private readonly FluentValidatorFactory _factory;

    public FluentValidationFactoryTests()
    {
        var serviceProviderMock = GetMockServiceProvider();
        _factory = new FluentValidatorFactory(serviceProviderMock);
    }

    private static IServiceProvider GetMockServiceProvider()
    {
        var validatorMock = new Mock<IValidator<string>>();
        validatorMock
            .Setup(v => v.Validate(It.IsAny<string>()))
            .Returns(new FluentValidation.Results.ValidationResult());

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(p => p.GetService(typeof(IValidator<string>)))
            .Returns(validatorMock.Object);

        return serviceProviderMock.Object;
    }

    [Fact]
    public void GetValidator_ReturnsValidator()
    {
        var validator = _factory.GetValidator<string>();
        Assert.NotNull(validator);
    }

    [Fact]
    public void GetValidator_Throws_WhenValidatorIsNotRegistered()
    {
        var ex = Record.Exception(_factory.GetValidator<object>);
        Assert.NotNull(ex);
        Assert.IsType<InvalidOperationException>(ex);
    }
}
