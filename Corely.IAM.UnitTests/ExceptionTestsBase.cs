namespace Corely.IAM.UnitTests;

public abstract class ExceptionTestsBase<TException>
    where TException : Exception, new()
{
    protected virtual bool HasDefaultConstructor => true;
    protected virtual bool HasMessageConstructor => true;
    protected virtual bool HasInnerExceptionConstructor => true;

    [Fact]
    public void DefaultConstructor_Works()
    {
        if (HasDefaultConstructor)
        {
            Activator.CreateInstance<TException>();
        }
    }

    [Fact]
    public void MessageConstructor_Works()
    {
        if (HasMessageConstructor)
        {
            Activator.CreateInstance(typeof(TException), "message");
        }
    }

    [Fact]
    public void MessageInnerExceptionConstructor_Works()
    {
        if (HasInnerExceptionConstructor)
        {
            Activator.CreateInstance(typeof(TException), "message", new Exception());
        }
    }
}
