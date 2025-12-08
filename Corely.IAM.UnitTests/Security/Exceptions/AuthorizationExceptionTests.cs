using Corely.IAM.Security.Exceptions;

namespace Corely.IAM.UnitTests.Security.Exceptions;

public class AuthorizationExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var exception = new AuthorizationException("group", "Create", 42);

        Assert.Equal("group", exception.ResourceType);
        Assert.Equal("Create", exception.RequiredAction);
        Assert.Equal(42, exception.ResourceId);
    }

    [Fact]
    public void Constructor_SetsMessage_WithResourceId()
    {
        var exception = new AuthorizationException("group", "Create", 42);

        Assert.Equal(
            "Authorization denied: Create permission required for group 42",
            exception.Message
        );
    }

    [Fact]
    public void Constructor_SetsMessage_WithoutResourceId()
    {
        var exception = new AuthorizationException("group", "Create");

        Assert.Equal(
            "Authorization denied: Create permission required for group",
            exception.Message
        );
    }

    [Fact]
    public void Constructor_WithInnerException_SetsInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new AuthorizationException("user", "Read", 1, inner);

        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void Constructor_ResourceIdNull_SetsPropertyToNull()
    {
        var exception = new AuthorizationException("role", "Delete", null);

        Assert.Null(exception.ResourceId);
    }

    [Fact]
    public void Constructor_InheritsFromException()
    {
        var exception = new AuthorizationException("group", "Create");

        Assert.IsType<Exception>(exception, exactMatch: false);
    }
}
