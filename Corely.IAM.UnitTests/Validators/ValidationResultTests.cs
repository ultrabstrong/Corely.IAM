using Corely.IAM.Validators;

namespace Corely.IAM.UnitTests.Validators;

public class ValidationResultTests
{
    [Fact]
    public void IsValid_ReturnsFalse_WhenErrorsIsNotNull()
    {
        var result = new ValidationResult { Errors = [new()] };

        Assert.False(result.IsValid);
    }

    [Fact]
    public void IsValid_ReturnsTrue_WhenErrorsIsNull()
    {
        var result = new ValidationResult { Errors = null };

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ThrowIfInvalid_Throws_WhenErrorsIsNotNull()
    {
        var result = new ValidationResult { Errors = [new()] };

        var ex = Record.Exception(result.ThrowIfInvalid);

        Assert.False(result.IsValid);
        Assert.NotNull(ex);
        Assert.IsType<ValidationException>(ex);
    }

    [Fact]
    public void ThrowIfInvalid_DoesNotThrow_WhenErrorsIsNull()
    {
        var result = new ValidationResult();
        Assert.True(result.IsValid);
        result.ThrowIfInvalid();
    }
}
