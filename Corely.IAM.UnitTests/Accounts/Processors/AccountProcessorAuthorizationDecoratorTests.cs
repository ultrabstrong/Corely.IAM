using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Platform.Providers;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.UnitTests.Accounts.Processors;

public class AccountProcessorAuthorizationDecoratorTests
{
    private readonly Mock<IAccountProcessor> _mockInnerProcessor = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly Mock<IUserContextProvider> _mockUserContextProvider = new();
    private readonly AccountProcessorAuthorizationDecorator _decorator;

    public AccountProcessorAuthorizationDecoratorTests()
    {
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(true);
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext(It.IsAny<Guid>())).Returns(true);
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(It.IsAny<Guid>(), It.IsAny<bool>()))
            .Returns(true);
        _decorator = new AccountProcessorAuthorizationDecorator(
            _mockInnerProcessor.Object,
            _mockAuthorizationProvider.Object,
            _mockUserContextProvider.Object
        );
    }

    [Fact]
    public async Task CreateAccount_CallsOwnUserAuthorization()
    {
        var request = new CreateAccountRequest("TestAccount", Guid.Empty);
        var expectedResult = new CreateAccountResult(
            CreateAccountResultCode.Success,
            "",
            Guid.CreateVersion7()
        );
        _mockInnerProcessor.Setup(x => x.CreateAccountAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.CreateAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(x => x.HasUserContext(), Times.Once);
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedForOwnUser(request.OwnerUserId, It.IsAny<bool>()),
            Times.Once
        );
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<Guid[]>()),
            Times.Never
        );
        _mockInnerProcessor.Verify(x => x.CreateAccountAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetAccountById_CallsAuthorizationProviderWithResourceId()
    {
        var accountId = Guid.CreateVersion7();
        var expectedResult = new GetAccountResult(
            GetAccountResultCode.Success,
            string.Empty,
            new Account { Id = accountId, AccountName = "TestAccount" }
        );
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedAsync(AuthAction.Read, Account.RESOURCE_TYPE, accountId))
            .ReturnsAsync(true);
        _mockInnerProcessor.Setup(x => x.GetAccountAsync(accountId)).ReturnsAsync(expectedResult);

        var result = await _decorator.GetAccountAsync(accountId);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(AuthAction.Read, Account.RESOURCE_TYPE, accountId),
            Times.Once
        );
    }

    [Fact]
    public async Task GetAccountById_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var accountId = Guid.CreateVersion7();
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedAsync(AuthAction.Read, Account.RESOURCE_TYPE, accountId))
            .ReturnsAsync(false);

        var result = await _decorator.GetAccountAsync(accountId);

        Assert.Equal(GetAccountResultCode.UnauthorizedError, result.ResultCode);
        Assert.Null(result.Account);
        _mockInnerProcessor.Verify(x => x.GetAccountAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAccount_CallsAuthorizationProvider()
    {
        var accountId = Guid.CreateVersion7();
        var expectedResult = new DeleteAccountResult(DeleteAccountResultCode.Success, "");
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedAsync(AuthAction.Delete, Account.RESOURCE_TYPE, accountId))
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.DeleteAccountAsync(accountId))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.DeleteAccountAsync(accountId);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(AuthAction.Delete, Account.RESOURCE_TYPE, accountId),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.DeleteAccountAsync(accountId), Times.Once);
    }

    [Fact]
    public async Task DeleteAccount_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var accountId = Guid.CreateVersion7();
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedAsync(AuthAction.Delete, Account.RESOURCE_TYPE, accountId))
            .ReturnsAsync(false);

        var result = await _decorator.DeleteAccountAsync(accountId);

        Assert.Equal(DeleteAccountResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(x => x.DeleteAccountAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task AddUserToAccount_RunsInSystemContext()
    {
        var request = new AddUserToAccountRequest(Guid.CreateVersion7(), Guid.CreateVersion7());
        var expectedResult = new AddUserToAccountResult(AddUserToAccountResultCode.Success, "");
        _mockUserContextProvider
            .Setup(x => x.GetUserContext())
            .Returns(new UserContext(true, "system"));
        _mockInnerProcessor
            .Setup(x => x.AddUserToAccountAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.AddUserToAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerProcessor.Verify(x => x.AddUserToAccountAsync(request), Times.Once);
    }

    [Fact]
    public async Task AddUserToAccount_IsRefused_ForAnAccountOwner()
    {
        var request = new AddUserToAccountRequest(Guid.CreateVersion7(), Guid.CreateVersion7());
        _mockUserContextProvider
            .Setup(x => x.GetUserContext())
            .Returns(
                new UserContext(
                    new User { Id = Guid.CreateVersion7(), Username = "owner" },
                    new Account { Id = request.AccountId, AccountName = "Acme" },
                    "device",
                    []
                )
            );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, Account.RESOURCE_TYPE, request.AccountId)
            )
            .ReturnsAsync(true);

        var result = await _decorator.AddUserToAccountAsync(request);

        Assert.Equal(AddUserToAccountResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.AddUserToAccountAsync(It.IsAny<AddUserToAccountRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RemoveUserFromAccount_Succeeds_WhenUserIsRemovingThemselves()
    {
        var request = new RemoveUserFromAccountRequest(
            Guid.CreateVersion7(),
            Guid.CreateVersion7()
        );
        var expectedResult = new RemoveUserFromAccountResult(
            RemoveUserFromAccountResultCode.Success,
            ""
        );
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(request.UserId, It.IsAny<bool>()))
            .Returns(true);
        _mockInnerProcessor
            .Setup(x => x.RemoveUserFromAccountAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RemoveUserFromAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<Guid[]>()),
            Times.Never
        );
        _mockInnerProcessor.Verify(x => x.RemoveUserFromAccountAsync(request), Times.Once);
    }

    [Fact]
    public async Task RemoveUserFromAccount_Succeeds_WhenUserHasAccountUpdatePermission()
    {
        var request = new RemoveUserFromAccountRequest(
            Guid.CreateVersion7(),
            Guid.CreateVersion7()
        );
        var expectedResult = new RemoveUserFromAccountResult(
            RemoveUserFromAccountResultCode.Success,
            ""
        );
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(request.UserId, It.IsAny<bool>()))
            .Returns(false);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, Account.RESOURCE_TYPE, request.AccountId)
            )
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.RemoveUserFromAccountAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RemoveUserFromAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(AuthAction.Update, Account.RESOURCE_TYPE, request.AccountId),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.RemoveUserFromAccountAsync(request), Times.Once);
    }

    [Fact]
    public async Task RemoveUserFromAccount_ReturnsUnauthorized_WhenNotOwnUserAndNoAccountPermission()
    {
        var request = new RemoveUserFromAccountRequest(
            Guid.CreateVersion7(),
            Guid.CreateVersion7()
        );
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(request.UserId, It.IsAny<bool>()))
            .Returns(false);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, Account.RESOURCE_TYPE, request.AccountId)
            )
            .ReturnsAsync(false);

        var result = await _decorator.RemoveUserFromAccountAsync(request);

        Assert.Equal(RemoveUserFromAccountResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.RemoveUserFromAccountAsync(It.IsAny<RemoveUserFromAccountRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetAccountKeys_CallsAuthorizationProvider()
    {
        var accountId = Guid.CreateVersion7();
        var expectedResult = new GetResult<AccountEntity>(
            RetrieveResultCode.Success,
            "",
            new AccountEntity { Id = accountId, AccountName = "Test" }
        );
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedAsync(AuthAction.Read, Account.RESOURCE_TYPE, accountId))
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.GetAccountKeysAsync(accountId))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.GetAccountKeysAsync(accountId);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(AuthAction.Read, Account.RESOURCE_TYPE, accountId),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.GetAccountKeysAsync(accountId), Times.Once);
    }

    [Fact]
    public async Task GetAccountKeys_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var accountId = Guid.CreateVersion7();
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedAsync(AuthAction.Read, Account.RESOURCE_TYPE, accountId))
            .ReturnsAsync(false);

        var result = await _decorator.GetAccountKeysAsync(accountId);

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        Assert.Null(result.Data);
        _mockInnerProcessor.Verify(x => x.GetAccountKeysAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AccountProcessorAuthorizationDecorator(
                null!,
                _mockAuthorizationProvider.Object,
                _mockUserContextProvider.Object
            )
        );

    [Fact]
    public void Constructor_ThrowsOnNullAuthorizationProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AccountProcessorAuthorizationDecorator(
                _mockInnerProcessor.Object,
                null!,
                _mockUserContextProvider.Object
            )
        );
}
