using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;

namespace Corely.IAM.Web.UnitTests.Pages.Management;

public class GroupListModelTests
{
    private readonly Mock<IRetrievalService> _mockRetrievalService = new();
    private readonly Mock<IRegistrationService> _mockRegistrationService = new();
    private readonly Mock<IDeregistrationService> _mockDeregistrationService = new();
    private readonly Web.Pages.Management.Groups.IndexModel _model;

    public GroupListModelTests()
    {
        _model = new Web.Pages.Management.Groups.IndexModel(
            _mockRetrievalService.Object,
            _mockRegistrationService.Object,
            _mockDeregistrationService.Object
        );
        _model.PageContext = PageTestHelpers.CreatePageContext();
    }

    [Fact]
    public async Task OnGetAsync_WithDefaults_PopulatesItemsAndTotalCount()
    {
        var groups = new List<Group>
        {
            new() { Id = Guid.CreateVersion7(), Name = "Admins" },
            new() { Id = Guid.CreateVersion7(), Name = "Editors" },
        };
        var paged = PagedResult<Group>.Create(groups, 2, 0, 25);
        _mockRetrievalService
            .Setup(s => s.ListGroupsAsync(null, null, 0, 25))
            .ReturnsAsync(new RetrieveListResult<Group>(RetrieveResultCode.Success, "ok", paged));

        await _model.OnGetAsync();

        Assert.Equal(2, _model.Items.Count);
        Assert.Equal(2, _model.TotalCount);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithValidName_ReturnsPageWithSuccessMessage()
    {
        _mockRegistrationService
            .Setup(s => s.RegisterGroupAsync(It.IsAny<RegisterGroupRequest>()))
            .ReturnsAsync(
                new RegisterGroupResult(CreateGroupResultCode.Success, null, Guid.CreateVersion7())
            );
        SetupEmptyList();

        var result = await _model.OnPostCreateAsync("NewGroup");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Group created successfully.", _model.Message);
        Assert.Equal("success", _model.MessageType);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithEmptyName_ReturnsPageWithDangerMessage()
    {
        SetupEmptyList();

        var result = await _model.OnPostCreateAsync("  ");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Group name is required.", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WithSuccess_ReturnsPageWithSuccessMessage()
    {
        var groupId = Guid.CreateVersion7();
        _mockDeregistrationService
            .Setup(s => s.DeregisterGroupAsync(It.IsAny<DeregisterGroupRequest>()))
            .ReturnsAsync(new DeregisterGroupResult(DeregisterGroupResultCode.Success, "ok"));
        SetupEmptyList();

        var result = await _model.OnPostDeleteAsync(groupId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Group deleted successfully.", _model.Message);
        Assert.Equal("success", _model.MessageType);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WithFailure_ReturnsPageWithDangerMessage()
    {
        var groupId = Guid.CreateVersion7();
        _mockDeregistrationService
            .Setup(s => s.DeregisterGroupAsync(It.IsAny<DeregisterGroupRequest>()))
            .ReturnsAsync(
                new DeregisterGroupResult(
                    DeregisterGroupResultCode.GroupNotFoundError,
                    "Group not found"
                )
            );
        SetupEmptyList();

        var result = await _model.OnPostDeleteAsync(groupId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Group not found", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithRegistrationFailure_ReturnsPageWithDangerMessage()
    {
        _mockRegistrationService
            .Setup(s => s.RegisterGroupAsync(It.IsAny<RegisterGroupRequest>()))
            .ReturnsAsync(
                new RegisterGroupResult(
                    CreateGroupResultCode.GroupExistsError,
                    "Group already exists",
                    Guid.Empty
                )
            );
        SetupEmptyList();

        var result = await _model.OnPostCreateAsync("ExistingGroup");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Group already exists", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    private void SetupEmptyList()
    {
        _mockRetrievalService
            .Setup(s => s.ListGroupsAsync(null, null, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(
                new RetrieveListResult<Group>(
                    RetrieveResultCode.Success,
                    "ok",
                    PagedResult<Group>.Empty()
                )
            );
    }
}
