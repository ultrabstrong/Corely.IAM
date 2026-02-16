using Corely.IAM.Users.Providers;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Corely.IAM.Web.Pages.Management;

public class DashboardModel(IUserContextProvider userContextProvider) : PageModel
{
    public bool IsAuthenticated { get; set; }

    public void OnGet()
    {
        IsAuthenticated = userContextProvider.GetUserContext()?.User != null;
    }
}
