using Corely.Common.Filtering;
using Corely.Common.Filtering.Filters;
using Corely.Common.Filtering.Ordering;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Web.Pages.Authentication;

public class SelectAccountModel(
    IAuthenticationService authenticationService,
    IUserContextProvider userContextProvider,
    IAuthCookieManager authCookieManager,
    IOptions<SecurityOptions> securityOptions,
    IRetrievalService retrievalService
) : PageModel
{
    private const int PageSize = 25;

    private readonly int _authTokenTtlSeconds = securityOptions.Value.AuthTokenTtlSeconds;

    public List<Account> Accounts { get; set; } = [];

    public int TotalCount { get; set; }

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userContext = userContextProvider.GetUserContext();
        if (userContext == null)
        {
            return Redirect(AppRoutes.SignIn);
        }

        await LoadAccountsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid accountId)
    {
        var result = await authenticationService.SwitchAccountAsync(
            new SwitchAccountRequest(accountId)
        );

        if (result.ResultCode != SignInResultCode.Success)
        {
            ErrorMessage = "Failed to switch account.";
            await LoadAccountsAsync();
            return Page();
        }

        authCookieManager.SetAuthCookies(
            Response.Cookies,
            result.AuthToken!,
            result.AuthTokenId!.Value,
            Request.IsHttps,
            _authTokenTtlSeconds
        );

        return Redirect(AppRoutes.Dashboard);
    }

    private async Task LoadAccountsAsync()
    {
        FilterBuilder<Account>? filter = null;
        if (!string.IsNullOrWhiteSpace(Search))
        {
            filter = Filter
                .For<Account>()
                .Where(a => a.AccountName, StringFilter.Contains(Search.Trim()));
        }

        var order = Order.For<Account>().By(a => a.AccountName, SortDirection.Ascending);

        var result = await retrievalService.ListAccountsAsync(
            filter,
            order,
            skip: (PageNumber - 1) * PageSize,
            take: PageSize
        );

        if (result.ResultCode == RetrieveResultCode.Success && result.Data != null)
        {
            Accounts = result.Data.Items;
            TotalCount = result.Data.TotalCount;
        }
    }
}
