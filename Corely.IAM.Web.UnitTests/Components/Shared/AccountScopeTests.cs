using Bunit.TestDoubles;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Users.Models;
using Corely.IAM.Web.Components.Shared;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.Web.UnitTests.Components.Shared;

public class AccountScopeTests : BunitContext
{
    private const string CHILD = "account content";
    private const string PAGE = "accounts/x/users?skip=25";

    private readonly Guid _routeAccountId = Guid.CreateVersion7();
    private readonly Mock<IBlazorUserContextAccessor> _userContextAccessor = new();

    public AccountScopeTests()
    {
        Services.AddSingleton(_userContextAccessor.Object);
        Services.GetRequiredService<NavigationManager>().NavigateTo(PAGE);
    }

    private void SignedInTo(Guid? currentAccountId) =>
        _userContextAccessor
            .Setup(x => x.GetUserContextAsync())
            .ReturnsAsync(
                PageTestHelpers.CreateUserContext(
                    currentAccount: currentAccountId is { } id
                        ? new Account { Id = id, AccountName = "Acme" }
                        : null
                )
            );

    private IRenderedComponent<AccountScope> RenderScope() =>
        Render<AccountScope>(p =>
            p.Add(x => x.AccountId, _routeAccountId).AddChildContent($"<p>{CHILD}</p>")
        );

    private string NavigatedTo() => Services.GetRequiredService<NavigationManager>().Uri;

    private static string Expected(string path) => $"http://localhost/{path}";

    [Fact]
    public void Render_ShowsTheChild_ForTheCurrentAccount()
    {
        SignedInTo(_routeAccountId);

        var cut = RenderScope();

        cut.WaitForAssertion(() => Assert.Contains(CHILD, cut.Markup));
        Assert.Equal(Expected(PAGE), NavigatedTo());
    }

    [Fact]
    public void Render_SwitchesAndComesBack_ForAnotherAccount()
    {
        SignedInTo(Guid.CreateVersion7());

        var cut = RenderScope();

        Assert.Equal(
            Expected(
                $"switch-account?accountId={_routeAccountId}&returnUrl={Uri.EscapeDataString("/" + PAGE)}"
            ),
            NavigatedTo()
        );
        Assert.DoesNotContain(CHILD, cut.Markup);
    }

    [Fact]
    public void Render_SwitchesAndComesBack_ForNoCurrentAccount()
    {
        SignedInTo(null);

        var cut = RenderScope();

        Assert.StartsWith(Expected($"switch-account?accountId={_routeAccountId}&"), NavigatedTo());
        Assert.DoesNotContain(CHILD, cut.Markup);
    }

    [Fact]
    public void Render_SignsInAndComesBack_ForNoUser()
    {
        _userContextAccessor.Setup(x => x.GetUserContextAsync()).ReturnsAsync((UserContext?)null);

        var cut = RenderScope();

        Assert.Equal(
            Expected($"signin?returnUrl={Uri.EscapeDataString("/" + PAGE)}"),
            NavigatedTo()
        );
        Assert.DoesNotContain(CHILD, cut.Markup);
    }

    [Fact]
    public void Render_SwitchesOnce_ForAParentThatRendersAgainBeforeTheBrowserLeaves()
    {
        SignedInTo(Guid.CreateVersion7());

        var cut = RenderScope();
        cut.Render(p => p.Add(x => x.AccountId, _routeAccountId).AddChildContent("<p>again</p>"));

        Assert.Single(
            Services.GetRequiredService<BunitNavigationManager>().History,
            h => h.Uri.Contains("switch-account", StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task Render_SwitchesOnce_ForAParentThatRendersAgainWhileTheContextLoads()
    {
        var context = new TaskCompletionSource<UserContext?>();
        _userContextAccessor.Setup(x => x.GetUserContextAsync()).Returns(() => context.Task);

        var cut = RenderScope();
        cut.Render(p => p.Add(x => x.AccountId, _routeAccountId).AddChildContent("<p>again</p>"));
        await cut.InvokeAsync(() =>
            context.SetResult(
                PageTestHelpers.CreateUserContext(
                    currentAccount: new Account { Id = Guid.CreateVersion7(), AccountName = "Acme" }
                )
            )
        );

        cut.WaitForAssertion(() =>
            Assert.Single(
                Services.GetRequiredService<BunitNavigationManager>().History,
                h => h.Uri.Contains("switch-account", StringComparison.Ordinal)
            )
        );
    }
}
