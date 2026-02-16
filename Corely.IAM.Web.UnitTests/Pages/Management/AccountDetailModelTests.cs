using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;

namespace Corely.IAM.Web.UnitTests.Pages.Management;

public class AccountDetailModelTests
{
    private readonly Mock<IRetrievalService> _mockRetrievalService = new();
    private readonly Mock<IModificationService> _mockModificationService = new();
    private readonly Mock<IRegistrationService> _mockRegistrationService = new();
    private readonly Mock<IDeregistrationService> _mockDeregistrationService = new();
    private readonly Web.Pages.Management.Accounts.DetailModel _model;

    public AccountDetailModelTests()
    {
        _model = new Web.Pages.Management.Accounts.DetailModel(
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
        var account = new Account
        {
            Id = id,
            AccountName = "Acme",
            Users = [new ChildRef(Guid.CreateVersion7(), "jdoe")],
        };
        _mockRetrievalService
            .Setup(s => s.GetAccountAsync(id, true))
            .ReturnsAsync(
                new RetrieveSingleResult<Account>(RetrieveResultCode.Success, "ok", account, null)
            );

        var result = await _model.OnGetAsync(id);

        Assert.IsType<PageResult>(result);
        Assert.Equal(id, _model.Id);
        Assert.Equal("Acme", _model.AccountName);
        Assert.Single(_model.Users);
    }

    [Fact]
    public async Task OnGetAsync_WhenNotFound_RedirectsToIndex()
    {
        var id = Guid.CreateVersion7();
        _mockRetrievalService
            .Setup(s => s.GetAccountAsync(id, true))
            .ReturnsAsync(
                new RetrieveSingleResult<Account>(
                    RetrieveResultCode.NotFoundError,
                    "not found",
                    null,
                    null
                )
            );

        var result = await _model.OnGetAsync(id);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Management/Accounts/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostEditAsync_WithSuccess_SetsSuccessMessage()
    {
        var id = Guid.CreateVersion7();
        _mockModificationService
            .Setup(s => s.ModifyAccountAsync(It.IsAny<UpdateAccountRequest>()))
            .ReturnsAsync(new ModifyResult(ModifyResultCode.Success, "ok"));
        SetupReload(id, "UpdatedName");

        var result = await _model.OnPostEditAsync(id, "UpdatedName");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Account updated successfully.", _model.Message);
        Assert.Equal("success", _model.MessageType);
    }

    [Fact]
    public async Task OnPostAddUserAsync_WithSuccess_SetsSuccessMessage()
    {
        var id = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        _mockRegistrationService
            .Setup(s => s.RegisterUserWithAccountAsync(It.IsAny<RegisterUserWithAccountRequest>()))
            .ReturnsAsync(
                new RegisterUserWithAccountResult(RegisterUserWithAccountResultCode.Success, null)
            );
        SetupReload(id, "Acme");

        var result = await _model.OnPostAddUserAsync(id, userId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("User added successfully.", _model.Message);
        Assert.Equal("success", _model.MessageType);
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_WithSuccess_SetsSuccessMessage()
    {
        var id = Guid.CreateVersion7();
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
        SetupReload(id, "Acme");

        var result = await _model.OnPostRemoveUserAsync(id, userId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("User removed successfully.", _model.Message);
        Assert.Equal("success", _model.MessageType);
    }

    [Fact]
    public async Task OnPostEditAsync_WithFailure_ReturnsPageWithDangerMessage()
    {
        var id = Guid.CreateVersion7();
        _mockModificationService
            .Setup(s => s.ModifyAccountAsync(It.IsAny<UpdateAccountRequest>()))
            .ReturnsAsync(new ModifyResult(ModifyResultCode.NotFoundError, "Account not found"));
        SetupReload(id, "Acme");

        var result = await _model.OnPostEditAsync(id, "UpdatedName");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Account not found", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_WithFailure_ReturnsPageWithDangerMessage()
    {
        var id = Guid.CreateVersion7();
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
        SetupReload(id, "Acme");

        var result = await _model.OnPostRemoveUserAsync(id, userId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("User not found", _model.Message);
        Assert.Equal("danger", _model.MessageType);
    }

    private void SetupReload(Guid id, string accountName)
    {
        _mockRetrievalService
            .Setup(s => s.GetAccountAsync(id, true))
            .ReturnsAsync(
                new RetrieveSingleResult<Account>(
                    RetrieveResultCode.Success,
                    "ok",
                    new Account { Id = id, AccountName = accountName },
                    null
                )
            );
    }
}
