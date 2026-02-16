using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;

namespace Corely.IAM.Web.UnitTests.Pages.Management;

public class AccountListModelTests
{
    private readonly Mock<IRetrievalService> _mockRetrievalService = new();
    private readonly Mock<IRegistrationService> _mockRegistrationService = new();
    private readonly Mock<IDeregistrationService> _mockDeregistrationService = new();
    private readonly Web.Pages.Management.Accounts.IndexModel _model;

    public AccountListModelTests()
    {
        _model = new Web.Pages.Management.Accounts.IndexModel(
            _mockRetrievalService.Object,
            _mockRegistrationService.Object,
            _mockDeregistrationService.Object
        );
        _model.PageContext = PageTestHelpers.CreatePageContext();
    }

    [Fact]
    public async Task OnGetAsync_WithDefaults_PopulatesItemsAndTotalCount()
    {
        var accounts = new List<Account>
        {
            new() { Id = Guid.CreateVersion7(), AccountName = "Acme" },
            new() { Id = Guid.CreateVersion7(), AccountName = "Globex" },
        };
        var paged = PagedResult<Account>.Create(accounts, 2, 0, 25);
        _mockRetrievalService
            .Setup(s => s.ListAccountsAsync(null, null, 0, 25))
            .ReturnsAsync(new RetrieveListResult<Account>(RetrieveResultCode.Success, "ok", paged));

        await _model.OnGetAsync();

        Assert.Equal(2, _model.Items.Count);
        Assert.Equal(2, _model.TotalCount);
        Assert.Equal(0, _model.Skip);
        Assert.Equal(25, _model.Take);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithValidName_ReturnsPageWithSuccessMessage()
    {
        _mockRegistrationService
            .Setup(s => s.RegisterAccountAsync(It.IsAny<RegisterAccountRequest>()))
            .ReturnsAsync(
                new RegisterAccountResult(
                    RegisterAccountResultCode.Success,
                    null,
                    Guid.CreateVersion7()
                )
            );
        SetupEmptyList();

        var result = await _model.OnPostCreateAsync("NewCorp");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Account created successfully.", _model.Message);
        Assert.Equal("success", _model.MessageType);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithEmptyName_ReturnsPageWithDangerMessage()
    {
        SetupEmptyList();

        var result = await _model.OnPostCreateAsync("  ");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Account name is required.", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WithSuccess_ReturnsPageWithSuccessMessage()
    {
        _mockDeregistrationService
            .Setup(s => s.DeregisterAccountAsync())
            .ReturnsAsync(new DeregisterAccountResult(DeregisterAccountResultCode.Success, "ok"));
        SetupEmptyList();

        var result = await _model.OnPostDeleteAsync(Guid.CreateVersion7());

        Assert.IsType<PageResult>(result);
        Assert.Equal("Account deleted successfully.", _model.Message);
        Assert.Equal("success", _model.MessageType);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WithFailure_ReturnsPageWithDangerMessage()
    {
        _mockDeregistrationService
            .Setup(s => s.DeregisterAccountAsync())
            .ReturnsAsync(
                new DeregisterAccountResult(
                    DeregisterAccountResultCode.AccountNotFoundError,
                    "Account not found"
                )
            );
        SetupEmptyList();

        var result = await _model.OnPostDeleteAsync(Guid.CreateVersion7());

        Assert.IsType<PageResult>(result);
        Assert.Equal("Account not found", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    [Fact]
    public async Task OnPostCreateAsync_WithRegistrationFailure_ReturnsPageWithDangerMessage()
    {
        _mockRegistrationService
            .Setup(s => s.RegisterAccountAsync(It.IsAny<RegisterAccountRequest>()))
            .ReturnsAsync(
                new RegisterAccountResult(
                    RegisterAccountResultCode.AccountCreationError,
                    "Account creation failed",
                    Guid.Empty
                )
            );
        SetupEmptyList();

        var result = await _model.OnPostCreateAsync("NewCorp");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Account creation failed", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    private void SetupEmptyList()
    {
        _mockRetrievalService
            .Setup(s => s.ListAccountsAsync(null, null, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(
                new RetrieveListResult<Account>(
                    RetrieveResultCode.Success,
                    "ok",
                    PagedResult<Account>.Empty()
                )
            );
    }
}
