using Corely.IAM.Accounts.Models;
using Corely.IAM.Users.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;

namespace Corely.IAM.Web.UnitTests.Helpers;

public static class PageTestHelpers
{
    public static PageContext CreatePageContext(HttpContext? httpContext = null)
    {
        httpContext ??= new DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new PageActionDescriptor()
        );
        return new PageContext(actionContext);
    }

    public static UserContext CreateUserContext(
        User? user = null,
        Account? currentAccount = null,
        string deviceId = "test-device",
        List<Account>? availableAccounts = null
    )
    {
        user ??= new User
        {
            Id = Guid.CreateVersion7(),
            Username = "testuser",
            Email = "test@test.com",
        };
        return new UserContext(user, currentAccount, deviceId, availableAccounts ?? []);
    }
}
