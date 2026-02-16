using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;

namespace Corely.IAM.Web.UnitTests.Pages.Management;

public class RoleDetailModelTests
{
    private readonly Mock<IRetrievalService> _mockRetrievalService = new();
    private readonly Mock<IModificationService> _mockModificationService = new();
    private readonly Mock<IRegistrationService> _mockRegistrationService = new();
    private readonly Mock<IDeregistrationService> _mockDeregistrationService = new();
    private readonly Web.Pages.Management.Roles.DetailModel _model;

    public RoleDetailModelTests()
    {
        _model = new Web.Pages.Management.Roles.DetailModel(
            _mockRetrievalService.Object,
            _mockModificationService.Object,
            _mockRegistrationService.Object,
            _mockDeregistrationService.Object
        );
        _model.PageContext = PageTestHelpers.CreatePageContext();
    }

    [Fact]
    public async Task OnGetAsync_WithValidId_PopulatesModel()
    {
        var id = Guid.CreateVersion7();
        var role = new Role
        {
            Id = id,
            Name = "Admin",
            Description = "Full access",
            Permissions = [new ChildRef(Guid.CreateVersion7(), "Read All")],
        };
        _mockRetrievalService
            .Setup(s => s.GetRoleAsync(id, true))
            .ReturnsAsync(
                new RetrieveSingleResult<Role>(RetrieveResultCode.Success, "ok", role, null)
            );

        var result = await _model.OnGetAsync(id);

        Assert.IsType<PageResult>(result);
        Assert.Equal(id, _model.Id);
        Assert.Equal("Admin", _model.Name);
        Assert.Equal("Full access", _model.Description);
        Assert.Single(_model.Permissions);
    }

    [Fact]
    public async Task OnGetAsync_WhenNotFound_RedirectsToIndex()
    {
        var id = Guid.CreateVersion7();
        _mockRetrievalService
            .Setup(s => s.GetRoleAsync(id, true))
            .ReturnsAsync(
                new RetrieveSingleResult<Role>(
                    RetrieveResultCode.NotFoundError,
                    "not found",
                    null,
                    null
                )
            );

        var result = await _model.OnGetAsync(id);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Management/Roles/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostEditAsync_WithSuccess_SetsSuccessMessage()
    {
        var id = Guid.CreateVersion7();
        _mockModificationService
            .Setup(s => s.ModifyRoleAsync(It.IsAny<UpdateRoleRequest>()))
            .ReturnsAsync(new ModifyResult(ModifyResultCode.Success, "ok"));
        SetupReload(id);

        var result = await _model.OnPostEditAsync(id, "UpdatedRole", "Updated desc");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Role updated successfully.", _model.Message);
        Assert.Equal("success", _model.MessageType);
    }

    [Fact]
    public async Task OnPostAddPermissionAsync_WithSuccess_SetsSuccessMessage()
    {
        var id = Guid.CreateVersion7();
        var permissionId = Guid.CreateVersion7();
        _mockRegistrationService
            .Setup(s =>
                s.RegisterPermissionsWithRoleAsync(It.IsAny<RegisterPermissionsWithRoleRequest>())
            )
            .ReturnsAsync(
                new RegisterPermissionsWithRoleResult(
                    AssignPermissionsToRoleResultCode.Success,
                    "ok",
                    1
                )
            );
        SetupReload(id);

        var result = await _model.OnPostAddPermissionAsync(id, permissionId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Permission added successfully.", _model.Message);
        Assert.Equal("success", _model.MessageType);
    }

    [Fact]
    public async Task OnPostRemovePermissionAsync_WithSuccess_SetsSuccessMessage()
    {
        var id = Guid.CreateVersion7();
        var permissionId = Guid.CreateVersion7();
        _mockDeregistrationService
            .Setup(s =>
                s.DeregisterPermissionsFromRoleAsync(
                    It.IsAny<DeregisterPermissionsFromRoleRequest>()
                )
            )
            .ReturnsAsync(
                new DeregisterPermissionsFromRoleResult(
                    DeregisterPermissionsFromRoleResultCode.Success,
                    "ok",
                    1,
                    []
                )
            );
        SetupReload(id);

        var result = await _model.OnPostRemovePermissionAsync(id, permissionId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Permission removed successfully.", _model.Message);
        Assert.Equal("success", _model.MessageType);
    }

    [Fact]
    public async Task OnPostEditAsync_WithFailure_ReturnsPageWithDangerMessage()
    {
        var id = Guid.CreateVersion7();
        _mockModificationService
            .Setup(s => s.ModifyRoleAsync(It.IsAny<UpdateRoleRequest>()))
            .ReturnsAsync(
                new ModifyResult(ModifyResultCode.SystemDefinedError, "Cannot modify system role")
            );
        SetupReload(id);

        var result = await _model.OnPostEditAsync(id, "UpdatedRole", "Updated desc");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Cannot modify system role", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    [Fact]
    public async Task OnPostRemovePermissionAsync_WithFailure_ReturnsPageWithDangerMessage()
    {
        var id = Guid.CreateVersion7();
        var permissionId = Guid.CreateVersion7();
        _mockDeregistrationService
            .Setup(s =>
                s.DeregisterPermissionsFromRoleAsync(
                    It.IsAny<DeregisterPermissionsFromRoleRequest>()
                )
            )
            .ReturnsAsync(
                new DeregisterPermissionsFromRoleResult(
                    DeregisterPermissionsFromRoleResultCode.RoleNotFoundError,
                    "Role not found",
                    0,
                    []
                )
            );
        SetupReload(id);

        var result = await _model.OnPostRemovePermissionAsync(id, permissionId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Role not found", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    private void SetupReload(Guid id)
    {
        _mockRetrievalService
            .Setup(s => s.GetRoleAsync(id, true))
            .ReturnsAsync(
                new RetrieveSingleResult<Role>(
                    RetrieveResultCode.Success,
                    "ok",
                    new Role { Id = id, Name = "TestRole" },
                    null
                )
            );
    }
}
