using System.Reflection;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Web.Components;
using Corely.IAM.Web.Components.Shared;
using Corely.IAM.Web.Components.Users;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Corely.IAM.Web.UnitTests.Components;

public class HostOwnedPathsTests : BunitContext
{
    private static readonly Assembly _assembly = typeof(AppRoutes).Assembly;

    [Fact]
    public void Assembly_HasNoRoutedBlazorPage()
    {
        var routed = _assembly.GetTypes().Where(t => t.IsDefined(typeof(RouteAttribute), false));

        Assert.Empty(routed.Select(t => t.FullName));
    }

    [Fact]
    public void AppRoutes_NamesOnlyTheAuthenticationEndpoints()
    {
        var names = typeof(AppRoutes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => f.Name)
            .Order();

        Assert.Equal(
            [
                "CreateAccount",
                "GoogleCallback",
                "Privacy",
                "Register",
                "RegisterWithGoogle",
                "SelectAccount",
                "SignIn",
                "SignOut",
                "SwitchAccount",
                "Terms",
                "VerifyMfa",
            ],
            names
        );
    }

    [Fact]
    public void UserList_RendersEveryLinkFromTheHost()
    {
        var accountId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var accessor = new Mock<IBlazorUserContextAccessor>();
        accessor
            .Setup(x => x.GetUserContextAsync())
            .ReturnsAsync(
                PageTestHelpers.CreateUserContext(
                    currentAccount: new Account { Id = accountId, AccountName = "Acme" }
                )
            );
        var retrieval = new Mock<IRetrievalService>();
        retrieval
            .Setup(x => x.ListUsersAsync(It.IsAny<ListUsersRequest>()))
            .ReturnsAsync(
                new RetrieveListResult<User>(
                    RetrieveResultCode.Success,
                    string.Empty,
                    PagedResult<User>.Create(
                        [
                            new User
                            {
                                Id = userId,
                                Username = "ann",
                                Email = "ann@example.com",
                            },
                        ],
                        1,
                        0,
                        25
                    )
                )
            );
        Services.AddSingleton<IIamWebLinks>(new FakeIamWebLinks());
        Services.AddSingleton(accessor.Object);
        Services.AddSingleton(retrieval.Object);
        Services.AddSingleton(new Mock<IDeregistrationService>().Object);
        Services.AddSingleton<ILogger<EntityPageBase>>(NullLogger<EntityPageBase>.Instance);
        ComponentFactories.AddStub<PermissionView>();
        ComponentFactories.AddStub<Pagination>();

        var cut = Render<UserList>();

        cut.WaitForAssertion(() =>
            Assert.Equal(
                [
                    $"{FakeIamWebLinks.PREFIX}/dashboard",
                    $"{FakeIamWebLinks.PREFIX}/user/{accountId}/{userId}",
                ],
                cut.FindAll("a[href]").Select(a => a.GetAttribute("href"))
            )
        );
    }
}
