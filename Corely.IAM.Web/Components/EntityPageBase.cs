using Corely.IAM.Web.Components.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Web.Components;

public abstract class EntityPageBase : AuthenticatedPageBase
{
    [Inject]
    protected ILogger<EntityPageBase> Logger { get; set; } = null!;

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
            Logger.LogError(ex, "An error occurred in page component");
            _message = "An unexpected error occurred. Please try again.";
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
            Logger.LogError(ex, "An error occurred in page component");
            _message = "An unexpected error occurred. Please try again.";
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
