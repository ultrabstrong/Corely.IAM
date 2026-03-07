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
}
