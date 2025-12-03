using Corely.IAM.Validators.Mappers;
using FluentValidation.Results;

namespace Corely.IAM.UnitTests.Validators.Mappers;

public class ValidationMapperTests
{
    [Fact]
    public void ToValidationError_ShouldMapAllProperties()
    {
        // Arrange
        var failure = new ValidationFailure("TestProperty", "Test error message");

        // Act
        var result = failure.ToValidationError();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test error message", result.Message);
        Assert.Equal("TestProperty", result.PropertyName);
    }

    [Theory]
    [InlineData("Property1", "Error1")]
    [InlineData("Property2", "Error2")]
    [InlineData("", "")]
    public void ToValidationError_ShouldMapVariousInputs(string propertyName, string errorMessage)
    {
        // Arrange
        var failure = new ValidationFailure(propertyName, errorMessage);

        // Act
        var result = failure.ToValidationError();

        // Assert
        Assert.Equal(errorMessage, result.Message);
        Assert.Equal(propertyName, result.PropertyName);
    }

    [Fact]
    public void ToValidationResult_ShouldMapValidResult()
    {
        // Arrange
        var fluentResult = new FluentValidation.Results.ValidationResult();

        // Act
        var result = fluentResult.ToValidationResult();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ToValidationResult_ShouldMapInvalidResult()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Property1", "Error1"),
            new("Property2", "Error2"),
        };
        var fluentResult = new FluentValidation.Results.ValidationResult(failures);

        // Act
        var result = fluentResult.ToValidationResult();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Equal(2, result.Errors.Count);
        Assert.Equal("Error1", result.Errors[0].Message);
        Assert.Equal("Property1", result.Errors[0].PropertyName);
        Assert.Equal("Error2", result.Errors[1].Message);
        Assert.Equal("Property2", result.Errors[1].PropertyName);
    }

    [Fact]
    public void ToValidationResult_ShouldMapMultipleErrors()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Username", "Username is required"),
            new("Email", "Invalid email format"),
            new("Password", "Password too weak"),
        };
        var fluentResult = new FluentValidation.Results.ValidationResult(failures);

        // Act
        var result = fluentResult.ToValidationResult();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors!.Count);
    }

    [Fact]
    public void ToValidationError_ToValidationResult_Integration()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("TestProp1", "TestError1"),
            new("TestProp2", "TestError2"),
        };
        var fluentResult = new FluentValidation.Results.ValidationResult(failures);

        // Act
        var result = fluentResult.ToValidationResult();

        // Assert
        Assert.Equal(failures.Count, result.Errors!.Count);
        for (int i = 0; i < failures.Count; i++)
        {
            Assert.Equal(failures[i].ErrorMessage, result.Errors[i].Message);
            Assert.Equal(failures[i].PropertyName, result.Errors[i].PropertyName);
        }
    }
}
