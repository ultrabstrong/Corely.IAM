namespace Corely.IAM.Web.Services;

public interface IAccountDisplayState
{
    string? AccountName { get; }
    event Action? OnChanged;
    void UpdateAccountName(string name);
}
