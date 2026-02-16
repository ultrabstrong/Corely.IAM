using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;

namespace Corely.IAM.Web.UnitTests.Pages.Management;

public class RoleListModelTests
{
    private readonly Mock<IRetrievalService> _mockRetrievalService = new();
    private readonly Mock<IRegistrationService> _mockRegistrationService = new();
    private readonly Mock<IDeregistrationService> _mockDeregistrationService = new();
    private readonly Web.Pages.Management.Roles.IndexModel _model;

    public RoleListModelTests()
    {
        _model = new Web.Pages.Management.Roles.IndexModel(
            _mockRetrievalService.Object,
            _mockRegistrationService.Object,
            _mockDeregistrationService.Object
        );
        _model.PageContext = PageTestHelpers.CreatePageContext();
    }

    [Fact]
    public async Task OnGetAsync_WithDefaults_PopulatesItemsAndTotalCount()
    {
        var roles = new List<Role>
        {
            new() { Id = Guid.CreateVersion7(), Name = "Admin" },
            new() { Id = Guid.CreateVersion7(), Name = "Editor" },
        };
        var paged = PagedResult<Role>.Create(roles, 2, 0, 25);
        _mockRetrievalService
            .Setup(s => s.ListRolesAsync(null, null, 0, 25))
            .ReturnsAsync(new RetrieveListResult<Role>(RetrieveResultCode.Success, "ok", paged));

        await _model.OnGetAsync();

        Assert.Equal(2, _model.Items.Count);
        Assert.Equal(2, _model.TotalCount);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithValidName_ReturnsPageWithSuccessMessage()
    {
        _mockRegistrationService
            .Setup(s => s.RegisterRoleAsync(It.IsAny<RegisterRoleRequest>()))
            .ReturnsAsync(
                new RegisterRoleResult(CreateRoleResultCode.Success, "ok", Guid.CreateVersion7())
            );
        SetupEmptyList();

        var result = await _model.OnPostCreateAsync("NewRole");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Role created successfully.", _model.Message);
        Assert.Equal("success", _model.MessageType);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithEmptyName_ReturnsPageWithDangerMessage()
    {
        SetupEmptyList();

        var result = await _model.OnPostCreateAsync("  ");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Role name is required.", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WithSuccess_ReturnsPageWithSuccessMessage()
    {
        var roleId = Guid.CreateVersion7();
        _mockDeregistrationService
            .Setup(s => s.DeregisterRoleAsync(It.IsAny<DeregisterRoleRequest>()))
            .ReturnsAsync(new DeregisterRoleResult(DeregisterRoleResultCode.Success, "ok"));
        SetupEmptyList();

        var result = await _model.OnPostDeleteAsync(roleId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Role deleted successfully.", _model.Message);
        Assert.Equal("success", _model.MessageType);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WithFailure_ReturnsPageWithDangerMessage()
    {
        var roleId = Guid.CreateVersion7();
        _mockDeregistrationService
            .Setup(s => s.DeregisterRoleAsync(It.IsAny<DeregisterRoleRequest>()))
            .ReturnsAsync(
                new DeregisterRoleResult(
                    DeregisterRoleResultCode.RoleNotFoundError,
                    "Role not found"
                )
            );
        SetupEmptyList();

        var result = await _model.OnPostDeleteAsync(roleId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Role not found", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithRegistrationFailure_ReturnsPageWithDangerMessage()
    {
        _mockRegistrationService
            .Setup(s => s.RegisterRoleAsync(It.IsAny<RegisterRoleRequest>()))
            .ReturnsAsync(
                new RegisterRoleResult(
                    CreateRoleResultCode.RoleExistsError,
                    "Role already exists",
                    Guid.Empty
                )
            );
        SetupEmptyList();

        var result = await _model.OnPostCreateAsync("ExistingRole");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Role already exists", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    private void SetupEmptyList()
    {
        _mockRetrievalService
            .Setup(s => s.ListRolesAsync(null, null, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(
                new RetrieveListResult<Role>(
                    RetrieveResultCode.Success,
                    "ok",
                    PagedResult<Role>.Empty()
                )
            );
    }
}
