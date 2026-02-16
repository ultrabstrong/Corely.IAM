using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;

namespace Corely.IAM.Web.UnitTests.Pages.Management;

public class UserListModelTests
{
    private readonly Mock<IRetrievalService> _mockRetrievalService = new();
    private readonly Mock<IDeregistrationService> _mockDeregistrationService = new();
    private readonly Web.Pages.Management.Users.IndexModel _model;

    public UserListModelTests()
    {
        _model = new Web.Pages.Management.Users.IndexModel(
            _mockRetrievalService.Object,
            _mockDeregistrationService.Object
        );
        _model.PageContext = PageTestHelpers.CreatePageContext();
    }

    [Fact]
    public async Task OnGetAsync_WithDefaults_PopulatesItemsAndTotalCount()
    {
        var users = new List<User>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                Username = "jdoe",
                Email = "jdoe@test.com",
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                Username = "asmith",
                Email = "asmith@test.com",
            },
        };
        var paged = PagedResult<User>.Create(users, 2, 0, 25);
        _mockRetrievalService
            .Setup(s => s.ListUsersAsync(null, null, 0, 25))
            .ReturnsAsync(new RetrieveListResult<User>(RetrieveResultCode.Success, "ok", paged));

        await _model.OnGetAsync();

        Assert.Equal(2, _model.Items.Count);
        Assert.Equal(2, _model.TotalCount);
        Assert.Equal(0, _model.Skip);
        Assert.Equal(25, _model.Take);
    }

    [Fact]
    public async Task OnGetAsync_WithCustomPaging_SetsSkipAndTake()
    {
        var paged = PagedResult<User>.Create([], 0, 10, 5);
        _mockRetrievalService
            .Setup(s => s.ListUsersAsync(null, null, 10, 5))
            .ReturnsAsync(new RetrieveListResult<User>(RetrieveResultCode.Success, "ok", paged));

        await _model.OnGetAsync(skip: 10, take: 5);

        Assert.Equal(10, _model.Skip);
        Assert.Equal(5, _model.Take);
    }

    [Fact]
    public async Task OnPostRemoveAsync_WithSuccess_ReturnsPageWithSuccessMessage()
    {
        var userId = Guid.CreateVersion7();
        _mockDeregistrationService
            .Setup(s =>
                s.DeregisterUserFromAccountAsync(It.IsAny<DeregisterUserFromAccountRequest>())
            )
            .ReturnsAsync(
                new DeregisterUserFromAccountResult(
                    DeregisterUserFromAccountResultCode.Success,
                    "ok"
                )
            );
        SetupEmptyList();

        var result = await _model.OnPostRemoveAsync(userId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("User removed from account.", _model.Message);
        Assert.Equal("success", _model.MessageType);
    }

    [Fact]
    public async Task OnPostRemoveAsync_WithFailure_ReturnsPageWithDangerMessage()
    {
        var userId = Guid.CreateVersion7();
        _mockDeregistrationService
            .Setup(s =>
                s.DeregisterUserFromAccountAsync(It.IsAny<DeregisterUserFromAccountRequest>())
            )
            .ReturnsAsync(
                new DeregisterUserFromAccountResult(
                    DeregisterUserFromAccountResultCode.UserNotFoundError,
                    "User not found"
                )
            );
        SetupEmptyList();

        var result = await _model.OnPostRemoveAsync(userId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("User not found", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    private void SetupEmptyList()
    {
        _mockRetrievalService
            .Setup(s => s.ListUsersAsync(null, null, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(
                new RetrieveListResult<User>(
                    RetrieveResultCode.Success,
                    "ok",
                    PagedResult<User>.Empty()
                )
            );
    }
}
