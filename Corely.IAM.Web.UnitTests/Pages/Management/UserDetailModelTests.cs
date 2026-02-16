using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;

namespace Corely.IAM.Web.UnitTests.Pages.Management;

public class UserDetailModelTests
{
    private readonly Mock<IRetrievalService> _mockRetrievalService = new();
    private readonly Mock<IModificationService> _mockModificationService = new();
    private readonly Mock<IRegistrationService> _mockRegistrationService = new();
    private readonly Mock<IDeregistrationService> _mockDeregistrationService = new();
    private readonly Web.Pages.Management.Users.DetailModel _model;

    public UserDetailModelTests()
    {
        _model = new Web.Pages.Management.Users.DetailModel(
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
        var user = new User
        {
            Id = id,
            Username = "jdoe",
            Email = "jdoe@test.com",
            Roles = [new ChildRef(Guid.CreateVersion7(), "Admin")],
        };
        _mockRetrievalService
            .Setup(s => s.GetUserAsync(id, true))
            .ReturnsAsync(
                new RetrieveSingleResult<User>(RetrieveResultCode.Success, "ok", user, null)
            );

        var result = await _model.OnGetAsync(id);

        Assert.IsType<PageResult>(result);
        Assert.Equal(id, _model.Id);
        Assert.Equal("jdoe", _model.Username);
        Assert.Equal("jdoe@test.com", _model.Email);
        Assert.Single(_model.Roles);
    }

    [Fact]
    public async Task OnGetAsync_WhenNotFound_RedirectsToIndex()
    {
        var id = Guid.CreateVersion7();
        _mockRetrievalService
            .Setup(s => s.GetUserAsync(id, true))
            .ReturnsAsync(
                new RetrieveSingleResult<User>(
                    RetrieveResultCode.NotFoundError,
                    "not found",
                    null,
                    null
                )
            );

        var result = await _model.OnGetAsync(id);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Management/Users/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostEditAsync_WithSuccess_SetsSuccessMessage()
    {
        var id = Guid.CreateVersion7();
        _mockModificationService
            .Setup(s => s.ModifyUserAsync(It.IsAny<UpdateUserRequest>()))
            .ReturnsAsync(new ModifyResult(ModifyResultCode.Success, "ok"));
        SetupReload(id);

        var result = await _model.OnPostEditAsync(id, "newname", "new@email.com");

        Assert.IsType<PageResult>(result);
        Assert.Equal("User updated successfully.", _model.Message);
        Assert.Equal("success", _model.MessageType);
    }

    [Fact]
    public async Task OnPostAddRoleAsync_WithSuccess_SetsSuccessMessage()
    {
        var id = Guid.CreateVersion7();
        var roleId = Guid.CreateVersion7();
        _mockRegistrationService
            .Setup(s => s.RegisterRolesWithUserAsync(It.IsAny<RegisterRolesWithUserRequest>()))
            .ReturnsAsync(
                new RegisterRolesWithUserResult(AssignRolesToUserResultCode.Success, "ok", 1)
            );
        SetupReload(id);

        var result = await _model.OnPostAddRoleAsync(id, roleId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Role assigned successfully.", _model.Message);
        Assert.Equal("success", _model.MessageType);
    }

    [Fact]
    public async Task OnPostRemoveRoleAsync_WithSuccess_SetsSuccessMessage()
    {
        var id = Guid.CreateVersion7();
        var roleId = Guid.CreateVersion7();
        _mockDeregistrationService
            .Setup(s => s.DeregisterRolesFromUserAsync(It.IsAny<DeregisterRolesFromUserRequest>()))
            .ReturnsAsync(
                new DeregisterRolesFromUserResult(
                    DeregisterRolesFromUserResultCode.Success,
                    "ok",
                    1,
                    []
                )
            );
        SetupReload(id);

        var result = await _model.OnPostRemoveRoleAsync(id, roleId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Role removed successfully.", _model.Message);
        Assert.Equal("success", _model.MessageType);
    }

    [Fact]
    public async Task OnPostEditAsync_WithFailure_ReturnsPageWithDangerMessage()
    {
        var id = Guid.CreateVersion7();
        _mockModificationService
            .Setup(s => s.ModifyUserAsync(It.IsAny<UpdateUserRequest>()))
            .ReturnsAsync(new ModifyResult(ModifyResultCode.NotFoundError, "User not found"));
        SetupReload(id);

        var result = await _model.OnPostEditAsync(id, "newname", "new@email.com");

        Assert.IsType<PageResult>(result);
        Assert.Equal("User not found", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    [Fact]
    public async Task OnPostAddRoleAsync_WithFailure_ReturnsPageWithDangerMessage()
    {
        var id = Guid.CreateVersion7();
        var roleId = Guid.CreateVersion7();
        _mockRegistrationService
            .Setup(s => s.RegisterRolesWithUserAsync(It.IsAny<RegisterRolesWithUserRequest>()))
            .ReturnsAsync(
                new RegisterRolesWithUserResult(
                    AssignRolesToUserResultCode.UserNotFoundError,
                    "User not found",
                    0
                )
            );
        SetupReload(id);

        var result = await _model.OnPostAddRoleAsync(id, roleId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("User not found", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    private void SetupReload(Guid id)
    {
        _mockRetrievalService
            .Setup(s => s.GetUserAsync(id, true))
            .ReturnsAsync(
                new RetrieveSingleResult<User>(
                    RetrieveResultCode.Success,
                    "ok",
                    new User
                    {
                        Id = id,
                        Username = "jdoe",
                        Email = "jdoe@test.com",
                    },
                    null
                )
            );
    }
}
