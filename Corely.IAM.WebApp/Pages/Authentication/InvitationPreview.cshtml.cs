using Corely.IAM.Invitations.Models;
using Corely.IAM.WebApp.Invitations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Corely.IAM.WebApp.Pages.Authentication;

public class InvitationPreviewModel(InvitationPreviewStore store, IWebHostEnvironment environment)
    : PageModel
{
    public InvitationDelivery? Delivery { get; } = store.Last;

    public string AcceptUrl =>
        $"{WebAppRoutes.AcceptInvitation}?token={Uri.EscapeDataString(Delivery?.Token ?? string.Empty)}";

    public IActionResult OnGet() => environment.IsDevelopment() ? Page() : NotFound();
}
