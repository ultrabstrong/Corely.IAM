using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;

namespace Corely.IAM.Web.UnitTests.Pages.Management;

public class GroupDetailModelTests
{
    private readonly Mock<IRetrievalService> _mockRetrievalService = new();
    private readonly Mock<IModificationService> _mockModificationService = new();
    private readonly Mock<IRegistrationService> _mockRegistrationService = new();
    private readonly Mock<IDeregistrationService> _mockDeregistrationService = new();
    private readonly Web.Pages.Management.Groups.DetailModel _model;

    public GroupDetailModelTests()
    {
        _model = new Web.Pages.Management.Groups.DetailModel(
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
        var group = new Group
        {
            Id = id,
            Name = "Admins",
            Description = "Admin group",
            Users = [new ChildRef(Guid.CreateVersion7(), "jdoe")],
            Roles = [new ChildRef(Guid.CreateVersion7(), "Admin")],
        };
        _mockRetrievalService
            .Setup(s => s.GetGroupAsync(id, true))
            .ReturnsAsync(
                new RetrieveSingleResult<Group>(RetrieveResultCode.Success, "ok", group, null)
            );

        var result = await _model.OnGetAsync(id);

        Assert.IsType<PageResult>(result);
        Assert.Equal(id, _model.Id);
        Assert.Equal("Admins", _model.Name);
        Assert.Equal("Admin group", _model.Description);
        Assert.Single(_model.Users);
        Assert.Single(_model.Roles);
    }

    [Fact]
    public async Task OnGetAsync_WhenNotFound_RedirectsToIndex()
    {
        var id = Guid.CreateVersion7();
        _mockRetrievalService
            .Setup(s => s.GetGroupAsync(id, true))
            .ReturnsAsync(
                new RetrieveSingleResult<Group>(
                    RetrieveResultCode.NotFoundError,
                    "not found",
                    null,
                    null
                )
            );

        var result = await _model.OnGetAsync(id);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Management/Groups/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostEditAsync_WithSuccess_SetsSuccessMessage()
    {
        var id = Guid.CreateVersion7();
        _mockModificationService
            .Setup(s => s.ModifyGroupAsync(It.IsAny<UpdateGroupRequest>()))
            .ReturnsAsync(new ModifyResult(ModifyResultCode.Success, "ok"));
        SetupReload(id);

        var result = await _model.OnPostEditAsync(id, "UpdatedGroup", "Updated desc");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Group updated successfully.", _model.Message);
        Assert.Equal("success", _model.MessageType);
    }

    [Fact]
    public async Task OnPostAddUserAsync_WithSuccess_SetsSuccessMessage()
    {
        var id = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        _mockRegistrationService
            .Setup(s => s.RegisterUsersWithGroupAsync(It.IsAny<RegisterUsersWithGroupRequest>()))
            .ReturnsAsync(
                new RegisterUsersWithGroupResult(AddUsersToGroupResultCode.Success, "ok", 1)
            );
        SetupReload(id);

        var result = await _model.OnPostAddUserAsync(id, userId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("User added successfully.", _model.Message);
        Assert.Equal("success", _model.MessageType);
    }

    [Fact]
    public async Task OnPostRemoveRoleAsync_WithSuccess_SetsSuccessMessage()
    {
        var id = Guid.CreateVersion7();
        var roleId = Guid.CreateVersion7();
        _mockDeregistrationService
            .Setup(s =>
                s.DeregisterRolesFromGroupAsync(It.IsAny<DeregisterRolesFromGroupRequest>())
            )
            .ReturnsAsync(
                new DeregisterRolesFromGroupResult(
                    DeregisterRolesFromGroupResultCode.Success,
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
            .Setup(s => s.ModifyGroupAsync(It.IsAny<UpdateGroupRequest>()))
            .ReturnsAsync(new ModifyResult(ModifyResultCode.NotFoundError, "Group not found"));
        SetupReload(id);

        var result = await _model.OnPostEditAsync(id, "UpdatedGroup", "Updated desc");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Group not found", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_WithFailure_ReturnsPageWithDangerMessage()
    {
        var id = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        _mockDeregistrationService
            .Setup(s =>
                s.DeregisterUsersFromGroupAsync(It.IsAny<DeregisterUsersFromGroupRequest>())
            )
            .ReturnsAsync(
                new DeregisterUsersFromGroupResult(
                    DeregisterUsersFromGroupResultCode.GroupNotFoundError,
                    "Group not found",
                    0,
                    []
                )
            );
        SetupReload(id);

        var result = await _model.OnPostRemoveUserAsync(id, userId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Group not found", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    private void SetupReload(Guid id)
    {
        _mockRetrievalService
            .Setup(s => s.GetGroupAsync(id, true))
            .ReturnsAsync(
                new RetrieveSingleResult<Group>(
                    RetrieveResultCode.Success,
                    "ok",
                    new Group { Id = id, Name = "TestGroup" },
                    null
                )
            );
    }
}
