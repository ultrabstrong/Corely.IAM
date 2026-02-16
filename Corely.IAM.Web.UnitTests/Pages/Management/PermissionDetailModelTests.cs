using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;

namespace Corely.IAM.Web.UnitTests.Pages.Management;

public class PermissionDetailModelTests
{
    private readonly Mock<IRetrievalService> _mockRetrievalService = new();
    private readonly Mock<IDeregistrationService> _mockDeregistrationService = new();
    private readonly Web.Pages.Management.Permissions.DetailModel _model;

    public PermissionDetailModelTests()
    {
        _model = new Web.Pages.Management.Permissions.DetailModel(
            _mockRetrievalService.Object,
            _mockDeregistrationService.Object
        );
        _model.PageContext = PageTestHelpers.CreatePageContext();
    }

    [Fact]
    public async Task OnGetAsync_WithValidId_PopulatesModel()
    {
        var id = Guid.CreateVersion7();
        var resourceId = Guid.CreateVersion7();
        var permission = new Permission
        {
            Id = id,
            ResourceType = "Account",
            ResourceId = resourceId,
            Description = "Full CRUD",
            Create = true,
            Read = true,
            Update = true,
            Delete = true,
            Execute = false,
        };
        _mockRetrievalService
            .Setup(s => s.GetPermissionAsync(id, true))
            .ReturnsAsync(
                new RetrieveSingleResult<Permission>(
                    RetrieveResultCode.Success,
                    "ok",
                    permission,
                    null
                )
            );

        var result = await _model.OnGetAsync(id);

        Assert.IsType<PageResult>(result);
        Assert.Equal(id, _model.Id);
        Assert.Equal("Account", _model.ResourceType);
        Assert.Equal(resourceId, _model.ResourceId);
        Assert.Equal("Full CRUD", _model.Description);
        Assert.True(_model.CanCreate);
        Assert.True(_model.CanRead);
        Assert.True(_model.CanUpdate);
        Assert.True(_model.CanDelete);
        Assert.False(_model.CanExecute);
    }

    [Fact]
    public async Task OnGetAsync_WhenNotFound_RedirectsToIndex()
    {
        var id = Guid.CreateVersion7();
        _mockRetrievalService
            .Setup(s => s.GetPermissionAsync(id, true))
            .ReturnsAsync(
                new RetrieveSingleResult<Permission>(
                    RetrieveResultCode.NotFoundError,
                    "not found",
                    null,
                    null
                )
            );

        var result = await _model.OnGetAsync(id);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Management/Permissions/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WithSuccess_RedirectsToIndex()
    {
        var id = Guid.CreateVersion7();
        _mockDeregistrationService
            .Setup(s => s.DeregisterPermissionAsync(It.IsAny<DeregisterPermissionRequest>()))
            .ReturnsAsync(
                new DeregisterPermissionResult(DeregisterPermissionResultCode.Success, "ok")
            );

        var result = await _model.OnPostDeleteAsync(id);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Management/Permissions/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WithFailure_ReturnsPageWithDangerMessage()
    {
        var id = Guid.CreateVersion7();
        _mockDeregistrationService
            .Setup(s => s.DeregisterPermissionAsync(It.IsAny<DeregisterPermissionRequest>()))
            .ReturnsAsync(
                new DeregisterPermissionResult(
                    DeregisterPermissionResultCode.PermissionNotFoundError,
                    "Permission not found"
                )
            );
        _mockRetrievalService
            .Setup(s => s.GetPermissionAsync(id, true))
            .ReturnsAsync(
                new RetrieveSingleResult<Permission>(
                    RetrieveResultCode.Success,
                    "ok",
                    new Permission
                    {
                        Id = id,
                        ResourceType = "Account",
                        ResourceId = Guid.Empty,
                    },
                    null
                )
            );

        var result = await _model.OnPostDeleteAsync(id);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Permission not found", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }
}
