using Corely.IAM.Security.Exceptions;

namespace Corely.IAM.UnitTests.Security.Exceptions;

public class UserContextNotSetExceptionTests : ExceptionTestsBase<UserContextNotSetException>
{
    [Fact]
    public void DefaultConstructor_SetsDefaultMessage()
    {
        var exception = new UserContextNotSetException();

        Assert.Equal(
            "User context is not set. Authentication is required before this operation.",
            exception.Message
        );
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var message = "Custom message";
        var exception = new UserContextNotSetException(message);

        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void Constructor_WithInnerException_SetsInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new UserContextNotSetException("message", inner);

        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void Constructor_InheritsFromException()
    {
        var exception = new UserContextNotSetException();

        Assert.IsType<Exception>(exception, exactMatch: false);
    }
}
