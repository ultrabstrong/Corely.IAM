namespace Corely.IAM.Web.Services;

public interface IAccountDisplayState
{
    string? AccountName { get; }
    event Action? OnChanged;
    void UpdateAccountName(string name);
}

public class AccountDisplayState : IAccountDisplayState
{
    public string? AccountName { get; private set; }
    public event Action? OnChanged;

    public void UpdateAccountName(string name)
    {
        AccountName = name;
        OnChanged?.Invoke();
    }
}
