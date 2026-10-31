using AngleSharp.Dom;
using Corely.IAM.Accounts.Models;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Providers;
using Corely.IAM.Platform.Models;
using Corely.IAM.Platform.Providers;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Web.Components;
using Corely.IAM.Web.Components.Roles;
using Corely.IAM.Web.Components.Shared;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Corely.IAM.Web.UnitTests.Pages.Roles;

public class RoleDetailTests : BunitContext
{
    private readonly Guid _accountId = Guid.CreateVersion7();
    private readonly Guid _roleId = Guid.CreateVersion7();
    private readonly Mock<IBlazorUserContextAccessor> _mockUserContextAccessor = new();
    private readonly Mock<IRetrievalService> _mockRetrievalService = new();
    private readonly Mock<IRegistrationService> _mockRegistrationService = new();
    private readonly Mock<IDeregistrationService> _mockDeregistrationService = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly Role _role;

    public RoleDetailTests()
    {
        _role = new Role
        {
            Id = _roleId,
            Name = "Editors",
            Users = [],
            Groups = [],
            ApiKeys = [],
            Permissions = [],
        };

        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IIamWebLinks>(new FakeIamWebLinks());
        Services.AddSingleton(_mockUserContextAccessor.Object);
        Services.AddSingleton(_mockRetrievalService.Object);
        Services.AddSingleton(_mockRegistrationService.Object);
        Services.AddSingleton(_mockDeregistrationService.Object);
        Services.AddSingleton(_mockAuthorizationProvider.Object);
        Services.AddSingleton(Mock.Of<IModificationService>());
        Services.AddSingleton(Mock.Of<IResourceTypeRegistry>());
        Services.AddSingleton<ILogger<EntityPageBase>>(NullLogger<EntityPageBase>.Instance);
        ComponentFactories.AddStub<EffectivePermissionsPanel>();

        _mockUserContextAccessor
            .Setup(x => x.GetUserContextAsync())
            .ReturnsAsync(
                PageTestHelpers.CreateUserContext(currentAccount: new Account { Id = _accountId })
            );
        _mockRetrievalService
            .Setup(x => x.GetRoleAsync(_roleId, true))
            .ReturnsAsync(
                new RetrieveSingleResult<Role>(
                    RetrieveResultCode.Success,
                    string.Empty,
                    _role,
                    null
                )
            );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<Guid[]>())
            )
            .ReturnsAsync(true);
        _mockAuthorizationProvider
            .Setup(x => x.CanGrantRolesAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(true);
        _mockAuthorizationProvider
            .Setup(x => x.GetAuthorizedResourceIdsAsync(It.IsAny<AuthAction>(), It.IsAny<string>()))
            .ReturnsAsync((IReadOnlySet<Guid>?)null);
    }

    private IRenderedComponent<RoleDetail> RenderPage(bool apiKeysEnabled = true)
    {
        Services.AddSingleton<ISecurityPolicyProvider>(
            new FixedSecurityPolicyProvider(
                SecurityPolicy.Default with
                {
                    ApiKeysEnabled = apiKeysEnabled,
                }
            )
        );
        var cut = Render<RoleDetail>(p => p.Add(x => x.Id, _roleId));
        cut.WaitForAssertion(() => cut.Find("h1"));
        cut.WaitForAssertion(() => Assert.Contains("Editors", cut.Find("h1").TextContent));
        return cut;
    }

    private static IElement? Button(IRenderedComponent<RoleDetail> cut, string text) =>
        cut.FindAll("button").FirstOrDefault(b => b.TextContent.Trim() == text);

    private static IElement Section(IRenderedComponent<RoleDetail> cut, string label) =>
        cut.FindAll(".section-divider")
            .First(s =>
                s.QuerySelector(".section-divider-label")!.TextContent.Trim().StartsWith(label)
            );

    private static void Pick(
        IRenderedComponent<RoleDetail> cut,
        string addButton,
        params string[] names
    )
    {
        Button(cut, addButton)!.Click();
        cut.WaitForAssertion(() =>
            Assert.Contains(names[0], cut.FindAll(".picker-item-text").Select(e => e.TextContent))
        );
        foreach (var name in names)
            cut.FindAll("label.list-group-item")
                .First(l => l.QuerySelector(".picker-item-text")!.TextContent == name)
                .QuerySelector("input")!
                .Change(true);
        cut.FindAll("button").First(b => b.TextContent.Contains("Add Selected")).Click();
    }

    private void ListUsers(params User[] users) =>
        _mockRetrievalService
            .Setup(x => x.ListUsersAsync(It.IsAny<ListUsersRequest>()))
            .ReturnsAsync(
                new RetrieveListResult<User>(
                    RetrieveResultCode.Success,
                    string.Empty,
                    PagedResult<User>.Create([.. users], users.Length, 0, 25)
                )
            );

    private void ListGroups(params Group[] groups) =>
        _mockRetrievalService
            .Setup(x => x.ListGroupsAsync(It.IsAny<ListGroupsRequest>()))
            .ReturnsAsync(
                new RetrieveListResult<Group>(
                    RetrieveResultCode.Success,
                    string.Empty,
                    PagedResult<Group>.Create([.. groups], groups.Length, 0, 25)
                )
            );

    private void ListApiKeys(params ApiKey[] apiKeys) =>
        _mockRetrievalService
            .Setup(x => x.ListApiKeysAsync(It.IsAny<ListApiKeysRequest>()))
            .ReturnsAsync(
                new RetrieveListResult<ApiKey>(
                    RetrieveResultCode.Success,
                    string.Empty,
                    PagedResult<ApiKey>.Create([.. apiKeys], apiKeys.Length, 0, 25)
                )
            );

    private void DenyUpdateOn(string resourceType) =>
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedAsync(AuthAction.Update, resourceType, It.IsAny<Guid[]>()))
            .ReturnsAsync(false);

    [Theory]
    [InlineData("Add Users")]
    [InlineData("Add Groups")]
    [InlineData("Add API keys")]
    public void AddAction_Shows_ForACallerWhoCanGrantTheRole(string button)
    {
        var cut = RenderPage();

        cut.WaitForAssertion(() => Assert.NotNull(Button(cut, button)));
    }

    [Theory]
    [InlineData("Add Users")]
    [InlineData("Add Groups")]
    [InlineData("Add API keys")]
    public void AddAction_Hides_ForACallerWhoCannotGrantTheRole(string button)
    {
        _mockAuthorizationProvider
            .Setup(x => x.CanGrantRolesAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(false);

        var cut = RenderPage();

        cut.WaitForAssertion(() => Assert.NotNull(Button(cut, "Add Permissions")));
        Assert.Null(Button(cut, button));
    }

    [Theory]
    [InlineData(User.RESOURCE_TYPE, "Add Users")]
    [InlineData(Group.RESOURCE_TYPE, "Add Groups")]
    [InlineData(ApiKey.RESOURCE_TYPE, "Add API keys")]
    public void AddAction_Hides_ForACallerWhoCannotUpdateThatType(
        string resourceType,
        string button
    )
    {
        DenyUpdateOn(resourceType);

        var cut = RenderPage();

        cut.WaitForAssertion(() => Assert.NotNull(Button(cut, "Add Permissions")));
        Assert.Null(Button(cut, button));
    }

    [Fact]
    public void ApiKeysSection_Hides_WhileThePlatformLeavesKeysOff()
    {
        var cut = RenderPage(apiKeysEnabled: false);

        cut.WaitForAssertion(() => Assert.NotNull(Button(cut, "Add Users")));
        Assert.Null(Button(cut, "Add API keys"));
    }

    [Fact]
    public void AddUsers_AssignsTheRoleInTheCurrentAccount_ForEachPickedUser()
    {
        var first = new User { Id = Guid.CreateVersion7(), Username = "ann" };
        var second = new User { Id = Guid.CreateVersion7(), Username = "bob" };
        ListUsers(first, second);
        _mockRegistrationService
            .Setup(x => x.RegisterRolesWithUserAsync(It.IsAny<RegisterRolesWithUserRequest>()))
            .ReturnsAsync(
                new RegisterRolesWithUserResult(AssignRolesToUserResultCode.Success, "", 1)
            );

        var cut = RenderPage();
        cut.WaitForAssertion(() => Assert.NotNull(Button(cut, "Add Users")));
        Pick(cut, "Add Users", "ann", "bob");

        cut.WaitForAssertion(() =>
            Assert.Contains("2 user(s) added.", cut.Find(".alert").TextContent)
        );
        foreach (var user in new[] { first, second })
            _mockRegistrationService.Verify(
                x =>
                    x.RegisterRolesWithUserAsync(
                        It.Is<RegisterRolesWithUserRequest>(r =>
                            r.UserId == user.Id
                            && r.AccountId == _accountId
                            && r.RoleIds.SequenceEqual(new[] { _roleId })
                        )
                    ),
                Times.Once
            );
    }

    [Fact]
    public void AddUsers_SaysHowManyWereAdded_WhenOneIsRefused()
    {
        var first = new User { Id = Guid.CreateVersion7(), Username = "ann" };
        var second = new User { Id = Guid.CreateVersion7(), Username = "bob" };
        ListUsers(first, second);
        _mockRegistrationService
            .SetupSequence(x =>
                x.RegisterRolesWithUserAsync(It.IsAny<RegisterRolesWithUserRequest>())
            )
            .ReturnsAsync(
                new RegisterRolesWithUserResult(AssignRolesToUserResultCode.Success, "", 1)
            )
            .ReturnsAsync(
                new RegisterRolesWithUserResult(
                    AssignRolesToUserResultCode.UnauthorizedError,
                    "Refused",
                    0
                )
            );

        var cut = RenderPage();
        cut.WaitForAssertion(() => Assert.NotNull(Button(cut, "Add Users")));
        Pick(cut, "Add Users", "ann", "bob");

        cut.WaitForAssertion(() =>
            Assert.Contains("1 of 2 user(s) added. Refused", cut.Find(".alert").TextContent)
        );
        _mockRetrievalService.Verify(x => x.GetRoleAsync(_roleId, true), Times.Exactly(2));
    }

    [Fact]
    public void AddGroups_AssignsTheRoleInTheCurrentAccount_ForAPickedGroup()
    {
        var group = new Group { Id = Guid.CreateVersion7(), Name = "Finance" };
        ListGroups(group);
        _mockRegistrationService
            .Setup(x => x.RegisterRolesWithGroupAsync(It.IsAny<RegisterRolesWithGroupRequest>()))
            .ReturnsAsync(
                new RegisterRolesWithGroupResult(AssignRolesToGroupResultCode.Success, "", 1)
            );

        var cut = RenderPage();
        cut.WaitForAssertion(() => Assert.NotNull(Button(cut, "Add Groups")));
        Pick(cut, "Add Groups", "Finance");

        cut.WaitForAssertion(() =>
            _mockRegistrationService.Verify(
                x =>
                    x.RegisterRolesWithGroupAsync(
                        It.Is<RegisterRolesWithGroupRequest>(r =>
                            r.GroupId == group.Id
                            && r.AccountId == _accountId
                            && r.RoleIds.SequenceEqual(new[] { _roleId })
                        )
                    ),
                Times.Once
            )
        );
    }

    [Fact]
    public void AddApiKeys_AssignsTheRoleInTheCurrentAccount_ForAPickedKey()
    {
        var apiKey = new ApiKey { Id = Guid.CreateVersion7(), Name = "Importer" };
        ListApiKeys(apiKey);
        _mockRegistrationService
            .Setup(x => x.RegisterRolesWithApiKeyAsync(It.IsAny<RegisterRolesWithApiKeyRequest>()))
            .ReturnsAsync(
                new RegisterRolesWithApiKeyResult(AssignRolesToApiKeyResultCode.Success, "", 1, [])
            );

        var cut = RenderPage();
        cut.WaitForAssertion(() => Assert.NotNull(Button(cut, "Add API keys")));
        Pick(cut, "Add API keys", "Importer");

        cut.WaitForAssertion(() =>
            _mockRegistrationService.Verify(
                x =>
                    x.RegisterRolesWithApiKeyAsync(
                        It.Is<RegisterRolesWithApiKeyRequest>(r =>
                            r.ApiKeyId == apiKey.Id
                            && r.AccountId == _accountId
                            && r.RoleIds.SequenceEqual(new[] { _roleId })
                        )
                    ),
                Times.Once
            )
        );
    }

    [Fact]
    public void UserPicker_OffersOnlyUsersWithoutTheRoleThatTheCallerCanUpdate()
    {
        var holder = new User { Id = Guid.CreateVersion7(), Username = "holder" };
        var allowed = new User { Id = Guid.CreateVersion7(), Username = "allowed" };
        var forbidden = new User { Id = Guid.CreateVersion7(), Username = "forbidden" };
        _role.Users = [new ChildRef(holder.Id, holder.Username)];
        ListUsers(holder, allowed, forbidden);
        _mockAuthorizationProvider
            .Setup(x => x.GetAuthorizedResourceIdsAsync(AuthAction.Update, User.RESOURCE_TYPE))
            .ReturnsAsync(new HashSet<Guid> { holder.Id, allowed.Id });

        var cut = RenderPage();
        cut.WaitForAssertion(() => Assert.NotNull(Button(cut, "Add Users")));
        Button(cut, "Add Users")!.Click();

        cut.WaitForAssertion(() =>
            Assert.Equal(["allowed"], cut.FindAll(".picker-item-text").Select(e => e.TextContent))
        );
    }

    [Fact]
    public void RemoveUser_TakesTheRoleFromTheUserInTheCurrentAccount_AfterConfirming()
    {
        var userId = Guid.CreateVersion7();
        _role.Users = [new ChildRef(userId, "ann")];
        _mockDeregistrationService
            .Setup(x => x.DeregisterRolesFromUserAsync(It.IsAny<DeregisterRolesFromUserRequest>()))
            .ReturnsAsync(
                new DeregisterRolesFromUserResult(
                    DeregisterRolesFromUserResultCode.Success,
                    "",
                    1,
                    []
                )
            );

        var cut = RenderPage();
        cut.WaitForAssertion(() =>
            Section(cut, "Users").QuerySelector("button[title='Remove']")!.Click()
        );
        cut.WaitForAssertion(() => Assert.Contains("'ann'", cut.Find(".modal-body").TextContent));
        cut.Find(".modal-footer .btn-danger").Click();

        cut.WaitForAssertion(() =>
            _mockDeregistrationService.Verify(
                x =>
                    x.DeregisterRolesFromUserAsync(
                        It.Is<DeregisterRolesFromUserRequest>(r =>
                            r.UserId == userId
                            && r.AccountId == _accountId
                            && r.RoleIds.SequenceEqual(new[] { _roleId })
                        )
                    ),
                Times.Once
            )
        );
    }

    [Fact]
    public void RemoveGroup_TakesTheRoleFromTheGroupInTheCurrentAccount_AfterConfirming()
    {
        var groupId = Guid.CreateVersion7();
        _role.Groups = [new ChildRef(groupId, "Finance")];
        _mockDeregistrationService
            .Setup(x =>
                x.DeregisterRolesFromGroupAsync(It.IsAny<DeregisterRolesFromGroupRequest>())
            )
            .ReturnsAsync(
                new DeregisterRolesFromGroupResult(
                    DeregisterRolesFromGroupResultCode.Success,
                    "",
                    1,
                    []
                )
            );

        var cut = RenderPage();
        cut.WaitForAssertion(() =>
            Section(cut, "Groups").QuerySelector("button[title='Remove']")!.Click()
        );
        cut.WaitForAssertion(() =>
            Assert.Contains("'Finance'", cut.Find(".modal-body").TextContent)
        );
        cut.Find(".modal-footer .btn-danger").Click();

        cut.WaitForAssertion(() =>
            _mockDeregistrationService.Verify(
                x =>
                    x.DeregisterRolesFromGroupAsync(
                        It.Is<DeregisterRolesFromGroupRequest>(r =>
                            r.GroupId == groupId
                            && r.AccountId == _accountId
                            && r.RoleIds.SequenceEqual(new[] { _roleId })
                        )
                    ),
                Times.Once
            )
        );
    }

    [Fact]
    public void RemoveApiKey_TakesTheRoleFromTheKeyInTheCurrentAccount_AfterConfirming()
    {
        var apiKeyId = Guid.CreateVersion7();
        _role.ApiKeys = [new ChildRef(apiKeyId, "Importer")];
        _mockDeregistrationService
            .Setup(x =>
                x.DeregisterRolesFromApiKeyAsync(It.IsAny<DeregisterRolesFromApiKeyRequest>())
            )
            .ReturnsAsync(
                new DeregisterRolesFromApiKeyResult(
                    DeregisterRolesFromApiKeyResultCode.Success,
                    "",
                    1,
                    []
                )
            );

        var cut = RenderPage();
        cut.WaitForAssertion(() =>
            Section(cut, "API keys").QuerySelector("button[title='Remove']")!.Click()
        );
        cut.WaitForAssertion(() =>
            Assert.Contains("'Importer'", cut.Find(".modal-body").TextContent)
        );
        cut.Find(".modal-footer .btn-danger").Click();

        cut.WaitForAssertion(() =>
            _mockDeregistrationService.Verify(
                x =>
                    x.DeregisterRolesFromApiKeyAsync(
                        It.Is<DeregisterRolesFromApiKeyRequest>(r =>
                            r.ApiKeyId == apiKeyId
                            && r.AccountId == _accountId
                            && r.RoleIds.SequenceEqual(new[] { _roleId })
                        )
                    ),
                Times.Once
            )
        );
    }

    [Fact]
    public void RemoveFailure_ShowsTheServiceMessage_ForASoleOwner()
    {
        var userId = Guid.CreateVersion7();
        _role.Users = [new ChildRef(userId, "ann")];
        _mockDeregistrationService
            .Setup(x => x.DeregisterRolesFromUserAsync(It.IsAny<DeregisterRolesFromUserRequest>()))
            .ReturnsAsync(
                new DeregisterRolesFromUserResult(
                    DeregisterRolesFromUserResultCode.UserIsSoleOwnerError,
                    "Sole owner",
                    0,
                    []
                )
            );

        var cut = RenderPage();
        cut.WaitForAssertion(() =>
            Section(cut, "Users").QuerySelector("button[title='Remove']")!.Click()
        );
        cut.WaitForAssertion(() => cut.Find(".modal-footer .btn-danger").Click());

        cut.WaitForAssertion(() =>
            Assert.Contains("Sole owner", cut.Find(".alert-danger").TextContent)
        );
    }

    [Fact]
    public void RemoveAction_Hides_ForAHolderTheCallerCannotUpdate()
    {
        var allowedId = Guid.CreateVersion7();
        var forbiddenId = Guid.CreateVersion7();
        _role.Users = [new ChildRef(allowedId, "allowed"), new ChildRef(forbiddenId, "forbidden")];
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Update,
                    User.RESOURCE_TYPE,
                    It.Is<Guid[]>(ids => ids.Contains(forbiddenId))
                )
            )
            .ReturnsAsync(false);

        var cut = RenderPage();

        cut.WaitForAssertion(() =>
            Assert.Single(Section(cut, "Users").QuerySelectorAll("button[title='Remove']"))
        );
        var rows = Section(cut, "Users").QuerySelectorAll("tbody tr");
        Assert.Contains(
            "allowed",
            rows.Single(r => r.QuerySelector("button[title='Remove']") != null).TextContent
        );
    }
}
