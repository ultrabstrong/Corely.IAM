using Corely.IAM.Models;
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
    protected bool _loading = true;

    protected bool _loadFailed;
    protected Guid _confirmItemId;
    protected string _confirmMessage = "";

    protected sealed override Task OnInitializedAuthenticatedAsync(IServiceProvider services) =>
        LoadAsync(() => LoadCoreAsync(services));

    /// <summary>Loads the page's data from the operation's services.</summary>
    protected abstract Task LoadCoreAsync(IServiceProvider services);

    /// <summary>
    /// Reloads the page in an operation of its own. After an action, this reads what the action
    /// wrote even when a service wrote it in SQL, which a read in the action's own scope would not.
    /// </summary>
    protected Task ReloadAsync() => LoadAsync(() => InOperationAsync(LoadCoreAsync));

    private async Task LoadAsync(Func<Task> load)
    {
        _loading = true;
        _loadFailed = false;
        try
        {
            await load();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred in page component");
            _message = "An unexpected error occurred. Please try again.";
            _messageType = AlertType.Danger;
            _loadFailed = true;
        }
        finally
        {
            _loading = false;
        }
    }

    protected void FailLoad(RetrieveResultCode resultCode, string resourceName)
    {
        _loadFailed = true;
        _message =
            resultCode == RetrieveResultCode.UnauthorizedError
                ? $"You don't have access to {resourceName} in this account."
                : $"The {resourceName} couldn't be loaded.";
        _messageType = AlertType.Warning;
    }

    /// <summary>
    /// Runs a user's action in an operation of its own, unless the page is busy. An action that
    /// changes data then calls <see cref="ReloadAsync"/>, which reads in a fresh operation.
    /// </summary>
    protected async Task ExecuteSafeAsync(Func<IServiceProvider, Task> action)
    {
        if (_loading)
            return;
        _loading = true;
        try
        {
            await InOperationAsync(action);
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
