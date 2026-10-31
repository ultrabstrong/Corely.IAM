using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Web.Components;
using Corely.IAM.Web.Components.Shared;
using Corely.IAM.Web.Components.Users;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Corely.IAM.Web.UnitTests.Pages.Users;

public class UserDetailGroupsTests : BunitContext
{
    private readonly Guid _accountId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Guid _groupId = Guid.CreateVersion7();
    private readonly Mock<IBlazorUserContextAccessor> _mockUserContextAccessor = new();
    private readonly Mock<IRetrievalService> _mockRetrievalService = new();
    private readonly Mock<IDeregistrationService> _mockDeregistrationService = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();

    public UserDetailGroupsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IIamWebLinks>(new FakeIamWebLinks());
        Services.AddSingleton(_mockUserContextAccessor.Object);
        Services.AddSingleton(_mockRetrievalService.Object);
        Services.AddSingleton(_mockDeregistrationService.Object);
        Services.AddSingleton(_mockAuthorizationProvider.Object);
        Services.AddSingleton(Mock.Of<IRegistrationService>());
        Services.AddSingleton<ILogger<EntityPageBase>>(NullLogger<EntityPageBase>.Instance);
        ComponentFactories.AddStub<EffectivePermissionsPanel>();

        _mockUserContextAccessor
            .Setup(x => x.GetUserContextAsync())
            .ReturnsAsync(
                PageTestHelpers.CreateUserContext(currentAccount: new Account { Id = _accountId })
            );
        _mockRetrievalService
            .Setup(x => x.GetUserAsync(_userId, true))
            .ReturnsAsync(
                new RetrieveSingleResult<User>(
                    RetrieveResultCode.Success,
                    string.Empty,
                    new User
                    {
                        Id = _userId,
                        Username = "ann",
                        Email = "ann@test.com",
                        Groups = [new ChildRef(_groupId, "Finance")],
                    },
                    null
                )
            );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<Guid[]>())
            )
            .ReturnsAsync(true);
    }

    [Fact]
    public void RemoveGroup_TakesTheUserOutOfTheGroupInTheCurrentAccount_AfterConfirming()
    {
        _mockDeregistrationService
            .Setup(x =>
                x.DeregisterUsersFromGroupAsync(It.IsAny<DeregisterUsersFromGroupRequest>())
            )
            .ReturnsAsync(
                new DeregisterUsersFromGroupResult(
                    DeregisterUsersFromGroupResultCode.Success,
                    "",
                    1,
                    []
                )
            );

        var cut = Render<UserDetail>(p => p.Add(x => x.Id, _userId));
        cut.WaitForAssertion(() => cut.Find("button[title='Remove']").Click());
        cut.WaitForAssertion(() =>
            Assert.Contains("'Finance'", cut.Find(".modal-body").TextContent)
        );
        cut.Find(".modal-footer .btn-danger").Click();

        cut.WaitForAssertion(() =>
            _mockDeregistrationService.Verify(
                x =>
                    x.DeregisterUsersFromGroupAsync(
                        It.Is<DeregisterUsersFromGroupRequest>(r =>
                            r.GroupId == _groupId
                            && r.AccountId == _accountId
                            && r.UserIds.SequenceEqual(new[] { _userId })
                        )
                    ),
                Times.Once
            )
        );
    }

    [Fact]
    public void RemoveGroup_Hides_ForAGroupTheCallerCannotUpdate()
    {
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Update,
                    Group.RESOURCE_TYPE,
                    It.Is<Guid[]>(ids => ids.Contains(_groupId))
                )
            )
            .ReturnsAsync(false);

        var cut = Render<UserDetail>(p => p.Add(x => x.Id, _userId));

        cut.WaitForAssertion(() => Assert.Contains("Finance", cut.Markup));
        Assert.Empty(cut.FindAll("button[title='Remove']"));
    }
}
