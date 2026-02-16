using Corely.IAM.Web.Components.Shared;

namespace Corely.IAM.Web.Components;

public abstract class EntityPageBase : AuthenticatedPageBase
{
    protected string? _message;
    protected AlertType _messageType = AlertType.Info;
    protected bool _loading;
    protected Guid _confirmItemId;
    protected string _confirmMessage = "";

    protected sealed override async Task OnInitializedAuthenticatedAsync() => await ReloadAsync();

    protected abstract Task LoadCoreAsync();

    protected async Task ReloadAsync()
    {
        _loading = true;
        try
        {
            await LoadCoreAsync();
        }
        catch (Exception ex)
        {
            _message = $"An error occurred: {ex.Message}";
            _messageType = AlertType.Danger;
        }
        finally
        {
            _loading = false;
        }
    }

    protected async Task ExecuteSafeAsync(Func<Task> action)
    {
        if (_loading)
            return;
        _loading = true;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _message = $"An error occurred: {ex.Message}";
            _messageType = AlertType.Danger;
        }
        finally
        {
            _loading = false;
        }
    }

    protected void SetResultMessage(
        bool success,
        string successMessage,
        string? failureMessage = null
    )
    {
        _message = success ? successMessage : (failureMessage ?? "Failed.");
        _messageType = success ? AlertType.Success : AlertType.Danger;
    }

    protected bool TryParseGuid(string? input, out Guid result)
    {
        if (Guid.TryParse(input, out result))
            return true;
        _message = "Invalid GUID.";
        _messageType = AlertType.Danger;
        return false;
    }

    protected void ShowConfirmation(ConfirmModal modal, Guid id, string message)
    {
        _confirmItemId = id;
        _confirmMessage = message;
        modal.Show();
    }
}
