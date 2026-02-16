using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;

namespace Corely.IAM.Web.UnitTests.Pages.Management;

public class PermissionListModelTests
{
    private readonly Mock<IRetrievalService> _mockRetrievalService = new();
    private readonly Mock<IRegistrationService> _mockRegistrationService = new();
    private readonly Mock<IDeregistrationService> _mockDeregistrationService = new();
    private readonly Web.Pages.Management.Permissions.IndexModel _model;

    public PermissionListModelTests()
    {
        _model = new Web.Pages.Management.Permissions.IndexModel(
            _mockRetrievalService.Object,
            _mockRegistrationService.Object,
            _mockDeregistrationService.Object
        );
        _model.PageContext = PageTestHelpers.CreatePageContext();
    }

    [Fact]
    public async Task OnGetAsync_WithDefaults_PopulatesItemsAndTotalCount()
    {
        var permissions = new List<Permission>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                ResourceType = "Account",
                ResourceId = Guid.Empty,
                Create = true,
                Read = true,
            },
        };
        var paged = PagedResult<Permission>.Create(permissions, 1, 0, 25);
        _mockRetrievalService
            .Setup(s => s.ListPermissionsAsync(null, null, 0, 25))
            .ReturnsAsync(
                new RetrieveListResult<Permission>(RetrieveResultCode.Success, "ok", paged)
            );

        await _model.OnGetAsync();

        Assert.Single(_model.Items);
        Assert.Equal(1, _model.TotalCount);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithValidData_ReturnsPageWithSuccessMessage()
    {
        _mockRegistrationService
            .Setup(s => s.RegisterPermissionAsync(It.IsAny<RegisterPermissionRequest>()))
            .ReturnsAsync(
                new RegisterPermissionResult(
                    CreatePermissionResultCode.Success,
                    "ok",
                    Guid.CreateVersion7()
                )
            );
        SetupEmptyList();

        var result = await _model.OnPostCreateAsync(
            "Account",
            null,
            true,
            true,
            false,
            false,
            false,
            "Test permission"
        );

        Assert.IsType<PageResult>(result);
        Assert.Equal("Permission created successfully.", _model.Message);
        Assert.Equal("success", _model.MessageType);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithEmptyResourceType_ReturnsPageWithDangerMessage()
    {
        SetupEmptyList();

        var result = await _model.OnPostCreateAsync(
            "  ",
            null,
            false,
            false,
            false,
            false,
            false,
            null
        );

        Assert.IsType<PageResult>(result);
        Assert.Equal("Resource type is required.", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithInvalidResourceId_ReturnsPageWithDangerMessage()
    {
        SetupEmptyList();

        var result = await _model.OnPostCreateAsync(
            "Account",
            "not-a-guid",
            false,
            false,
            false,
            false,
            false,
            null
        );

        Assert.IsType<PageResult>(result);
        Assert.Equal("Invalid Resource ID format.", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WithSuccess_ReturnsPageWithSuccessMessage()
    {
        var permissionId = Guid.CreateVersion7();
        _mockDeregistrationService
            .Setup(s => s.DeregisterPermissionAsync(It.IsAny<DeregisterPermissionRequest>()))
            .ReturnsAsync(
                new DeregisterPermissionResult(DeregisterPermissionResultCode.Success, "ok")
            );
        SetupEmptyList();

        var result = await _model.OnPostDeleteAsync(permissionId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Permission deleted successfully.", _model.Message);
        Assert.Equal("success", _model.MessageType);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WithFailure_ReturnsPageWithDangerMessage()
    {
        var permissionId = Guid.CreateVersion7();
        _mockDeregistrationService
            .Setup(s => s.DeregisterPermissionAsync(It.IsAny<DeregisterPermissionRequest>()))
            .ReturnsAsync(
                new DeregisterPermissionResult(
                    DeregisterPermissionResultCode.PermissionNotFoundError,
                    "Permission not found"
                )
            );
        SetupEmptyList();

        var result = await _model.OnPostDeleteAsync(permissionId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Permission not found", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithRegistrationFailure_ReturnsPageWithDangerMessage()
    {
        _mockRegistrationService
            .Setup(s => s.RegisterPermissionAsync(It.IsAny<RegisterPermissionRequest>()))
            .ReturnsAsync(
                new RegisterPermissionResult(
                    CreatePermissionResultCode.PermissionExistsError,
                    "Permission already exists",
                    Guid.Empty
                )
            );
        SetupEmptyList();

        var result = await _model.OnPostCreateAsync(
            "Account",
            null,
            true,
            true,
            false,
            false,
            false,
            "Test permission"
        );

        Assert.IsType<PageResult>(result);
        Assert.Equal("Permission already exists", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    private void SetupEmptyList()
    {
        _mockRetrievalService
            .Setup(s => s.ListPermissionsAsync(null, null, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(
                new RetrieveListResult<Permission>(
                    RetrieveResultCode.Success,
                    "ok",
                    PagedResult<Permission>.Empty()
                )
            );
    }
}
