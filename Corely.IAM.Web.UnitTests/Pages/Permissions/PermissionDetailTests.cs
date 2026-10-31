using AngleSharp.Dom;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Web.Components;
using Corely.IAM.Web.Components.Permissions;
using Corely.IAM.Web.Components.Shared;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Corely.IAM.Web.UnitTests.Pages.Permissions;

public class PermissionDetailTests : BunitContext
{
    private readonly Guid _accountId = Guid.CreateVersion7();
    private readonly Guid _permissionId = Guid.CreateVersion7();
    private readonly Mock<IBlazorUserContextAccessor> _mockUserContextAccessor = new();
    private readonly Mock<IRetrievalService> _mockRetrievalService = new();
    private readonly Mock<IRegistrationService> _mockRegistrationService = new();
    private readonly Mock<IDeregistrationService> _mockDeregistrationService = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly Permission _permission;

    public PermissionDetailTests()
    {
        _permission = new Permission
        {
            Id = _permissionId,
            ResourceType = "group",
            Read = true,
            Roles = [],
        };

        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IIamWebLinks>(new FakeIamWebLinks());
        Services.AddSingleton(_mockUserContextAccessor.Object);
        Services.AddSingleton(_mockRetrievalService.Object);
        Services.AddSingleton(_mockRegistrationService.Object);
        Services.AddSingleton(_mockDeregistrationService.Object);
        Services.AddSingleton(_mockAuthorizationProvider.Object);
        Services.AddSingleton(Mock.Of<IModificationService>());
        Services.AddSingleton<ILogger<EntityPageBase>>(NullLogger<EntityPageBase>.Instance);
        ComponentFactories.AddStub<EffectivePermissionsPanel>();

        _mockUserContextAccessor
            .Setup(x => x.GetUserContextAsync())
            .ReturnsAsync(
                PageTestHelpers.CreateUserContext(currentAccount: new Account { Id = _accountId })
            );
        _mockRetrievalService
            .Setup(x => x.GetPermissionAsync(_permissionId, true))
            .ReturnsAsync(
                new RetrieveSingleResult<Permission>(
                    RetrieveResultCode.Success,
                    string.Empty,
                    _permission,
                    null
                )
            );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<Guid[]>())
            )
            .ReturnsAsync(true);
        _mockAuthorizationProvider
            .Setup(x => x.CanGrantPermissionsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(true);
        _mockAuthorizationProvider
            .Setup(x => x.GetAuthorizedResourceIdsAsync(It.IsAny<AuthAction>(), It.IsAny<string>()))
            .ReturnsAsync((IReadOnlySet<Guid>?)null);
    }

    private IRenderedComponent<PermissionDetail> RenderPage()
    {
        var cut = Render<PermissionDetail>(p => p.Add(x => x.Id, _permissionId));
        cut.WaitForAssertion(() => cut.Find(".detail-section"));
        return cut;
    }

    private static IElement? Button(IRenderedComponent<PermissionDetail> cut, string text) =>
        cut.FindAll("button").FirstOrDefault(b => b.TextContent.Trim() == text);

    private void ListRoles(params Role[] roles) =>
        _mockRetrievalService
            .Setup(x => x.ListRolesAsync(It.IsAny<ListRolesRequest>()))
            .ReturnsAsync(
                new RetrieveListResult<Role>(
                    RetrieveResultCode.Success,
                    string.Empty,
                    PagedResult<Role>.Create([.. roles], roles.Length, 0, 25)
                )
            );

    [Fact]
    public void AddToRoles_Shows_ForACallerWhoCanGrantThePermission()
    {
        var cut = RenderPage();

        cut.WaitForAssertion(() => Assert.NotNull(Button(cut, "Add to Roles")));
    }

    [Fact]
    public void AddToRoles_Hides_ForACallerWhoCannotGrantThePermission()
    {
        _mockAuthorizationProvider
            .Setup(x => x.CanGrantPermissionsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(false);

        var cut = RenderPage();

        cut.WaitForAssertion(() => Assert.Contains("Roles", cut.Markup));
        Assert.Null(Button(cut, "Add to Roles"));
    }

    [Fact]
    public void AddToRoles_Hides_ForACallerWhoCannotUpdateRoles()
    {
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, Role.RESOURCE_TYPE, It.IsAny<Guid[]>())
            )
            .ReturnsAsync(false);

        var cut = RenderPage();

        cut.WaitForAssertion(() => Assert.Contains("No roles hold", cut.Markup));
        Assert.Null(Button(cut, "Add to Roles"));
    }

    [Fact]
    public void RolesSection_Hides_ForACallerWhoCannotReadRoles()
    {
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, Role.RESOURCE_TYPE, It.IsAny<Guid[]>())
            )
            .ReturnsAsync(false);

        var cut = RenderPage();

        Assert.DoesNotContain("No roles hold", cut.Markup);
    }

    [Fact]
    public void AddToRoles_AttachesThePermissionInTheCurrentAccount_ForEachPickedRole()
    {
        var first = new Role { Id = Guid.CreateVersion7(), Name = "Editors" };
        var second = new Role { Id = Guid.CreateVersion7(), Name = "Viewers" };
        ListRoles(first, second);
        _mockRegistrationService
            .Setup(x =>
                x.RegisterPermissionsWithRoleAsync(It.IsAny<RegisterPermissionsWithRoleRequest>())
            )
            .ReturnsAsync(
                new RegisterPermissionsWithRoleResult(
                    AssignPermissionsToRoleResultCode.Success,
                    "",
                    1
                )
            );

        var cut = RenderPage();
        cut.WaitForAssertion(() => Assert.NotNull(Button(cut, "Add to Roles")));
        Button(cut, "Add to Roles")!.Click();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("label.list-group-item").Count));
        for (var row = 0; row < 2; row++)
            cut.FindAll("label.list-group-item")[row].QuerySelector("input")!.Change(true);
        cut.FindAll("button").First(b => b.TextContent.Contains("Add Selected")).Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Added to 2 role(s).", cut.Find(".alert").TextContent)
        );
        foreach (var role in new[] { first, second })
            _mockRegistrationService.Verify(
                x =>
                    x.RegisterPermissionsWithRoleAsync(
                        It.Is<RegisterPermissionsWithRoleRequest>(r =>
                            r.RoleId == role.Id
                            && r.AccountId == _accountId
                            && r.PermissionIds.SequenceEqual(new[] { _permissionId })
                        )
                    ),
                Times.Once
            );
    }

    [Fact]
    public void RolePicker_OffersOnlyRolesWithoutThePermissionThatTheCallerCanUpdate()
    {
        var holder = new Role { Id = Guid.CreateVersion7(), Name = "holder" };
        var allowed = new Role { Id = Guid.CreateVersion7(), Name = "allowed" };
        var forbidden = new Role { Id = Guid.CreateVersion7(), Name = "forbidden" };
        _permission.Roles = [new ChildRef(holder.Id, holder.Name)];
        ListRoles(holder, allowed, forbidden);
        _mockAuthorizationProvider
            .Setup(x => x.GetAuthorizedResourceIdsAsync(AuthAction.Update, Role.RESOURCE_TYPE))
            .ReturnsAsync(new HashSet<Guid> { holder.Id, allowed.Id });

        var cut = RenderPage();
        cut.WaitForAssertion(() => Assert.NotNull(Button(cut, "Add to Roles")));
        Button(cut, "Add to Roles")!.Click();

        cut.WaitForAssertion(() =>
            Assert.Equal(["allowed"], cut.FindAll(".picker-item-text").Select(e => e.TextContent))
        );
    }

    [Fact]
    public void Remove_DetachesThePermissionInTheCurrentAccount_AfterConfirming()
    {
        var roleId = Guid.CreateVersion7();
        _permission.Roles = [new ChildRef(roleId, "Editors")];
        _mockDeregistrationService
            .Setup(x =>
                x.DeregisterPermissionsFromRoleAsync(
                    It.IsAny<DeregisterPermissionsFromRoleRequest>()
                )
            )
            .ReturnsAsync(
                new DeregisterPermissionsFromRoleResult(
                    DeregisterPermissionsFromRoleResultCode.Success,
                    "",
                    1,
                    []
                )
            );

        var cut = RenderPage();
        cut.WaitForAssertion(() => cut.Find("button[title='Remove']").Click());
        cut.WaitForAssertion(() =>
            Assert.Contains("'Editors'", cut.Find(".modal-body").TextContent)
        );
        cut.Find(".modal-footer .btn-danger").Click();

        cut.WaitForAssertion(() =>
            _mockDeregistrationService.Verify(
                x =>
                    x.DeregisterPermissionsFromRoleAsync(
                        It.Is<DeregisterPermissionsFromRoleRequest>(r =>
                            r.RoleId == roleId
                            && r.AccountId == _accountId
                            && r.PermissionIds.SequenceEqual(new[] { _permissionId })
                        )
                    ),
                Times.Once
            )
        );
    }

    [Fact]
    public void Remove_Hides_ForARoleTheCallerCannotUpdate()
    {
        var roleId = Guid.CreateVersion7();
        _permission.Roles = [new ChildRef(roleId, "Editors")];
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Update,
                    Role.RESOURCE_TYPE,
                    It.Is<Guid[]>(ids => ids.Contains(roleId))
                )
            )
            .ReturnsAsync(false);

        var cut = RenderPage();

        cut.WaitForAssertion(() => Assert.Contains("Editors", cut.Markup));
        Assert.Empty(cut.FindAll("button[title='Remove']"));
    }

    [Fact]
    public void Remove_Hides_ForASystemPermissionOnTheOwnerRole()
    {
        typeof(Permission)
            .GetProperty(nameof(Permission.IsSystemDefined))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(_permission, [true]);
        _permission.Roles = [new ChildRef(Guid.CreateVersion7(), Role.OWNER_ROLE_NAME, true)];

        var cut = RenderPage();

        cut.WaitForAssertion(() => Assert.Contains(Role.OWNER_ROLE_NAME, cut.Markup));
        Assert.Empty(cut.FindAll("button[title='Remove']"));
    }
}
