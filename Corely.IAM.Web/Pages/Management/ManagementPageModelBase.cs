using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Corely.IAM.Web.Pages.Management;

public abstract class ManagementPageModelBase : PageModel
{
    public string? Message { get; set; }
    public string MessageType { get; set; } = "info";

    protected void SetResultMessage(
        bool success,
        string successMessage,
        string? failureMessage = null
    )
    {
        Message = success ? successMessage : (failureMessage ?? "Failed.");
        MessageType = success ? "success" : "danger";
    }
}
