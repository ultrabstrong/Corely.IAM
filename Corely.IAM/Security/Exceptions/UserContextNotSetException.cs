namespace Corely.IAM.Security.Exceptions;

public class UserContextNotSetException : Exception
{
    public UserContextNotSetException()
        : base("User context is not set. Authentication is required before this operation.") { }

    public UserContextNotSetException(string message)
        : base(message) { }

    public UserContextNotSetException(string message, Exception innerException)
        : base(message, innerException) { }
}
