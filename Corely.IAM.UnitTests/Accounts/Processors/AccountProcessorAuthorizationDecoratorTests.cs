using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.UnitTests.Accounts.Processors;

public class AccountProcessorAuthorizationDecoratorTests
{
    private readonly Mock<IAccountProcessor> _mockInnerProcessor = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly AccountProcessorAuthorizationDecorator _decorator;

    public AccountProcessorAuthorizationDecoratorTests()
    {
        _decorator = new AccountProcessorAuthorizationDecorator(
            _mockInnerProcessor.Object,
            _mockAuthorizationProvider.Object
        );
    }

    [Fact]
    public async Task CreateAccountAsync_BypassesAuthorization()
    {
        var request = new CreateAccountRequest("TestAccount", 1);
        var expectedResult = new CreateAccountResult(
            CreateAccountResultCode.Success,
            "",
            1,
            Guid.NewGuid()
        );
        _mockInnerProcessor.Setup(x => x.CreateAccountAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.CreateAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<int?>()),
            Times.Never
        );
        _mockInnerProcessor.Verify(x => x.CreateAccountAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetAccountAsyncById_CallsAuthorizationProviderWithResourceId()
    {
        var accountId = 5;
        var expectedResult = new GetAccountResult(
            GetAccountResultCode.Success,
            string.Empty,
            new Account { Id = accountId, AccountName = "TestAccount" }
        );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Read,
                    PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                    accountId
                )
            )
            .ReturnsAsync(true);
        _mockInnerProcessor.Setup(x => x.GetAccountAsync(accountId)).ReturnsAsync(expectedResult);

        var result = await _decorator.GetAccountAsync(accountId);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(
                    AuthAction.Read,
                    PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                    accountId
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetAccountAsyncById_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var accountId = 5;
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Read,
                    PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                    accountId
                )
            )
            .ReturnsAsync(false);

        var result = await _decorator.GetAccountAsync(accountId);

        Assert.Equal(GetAccountResultCode.UnauthorizedError, result.ResultCode);
        Assert.Null(result.Account);
        _mockInnerProcessor.Verify(x => x.GetAccountAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAccountAsync_CallsAuthorizationProvider()
    {
        var accountId = 5;
        var expectedResult = new DeleteAccountResult(DeleteAccountResultCode.Success, "");
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Delete,
                    PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                    accountId
                )
            )
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.DeleteAccountAsync(accountId))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.DeleteAccountAsync(accountId);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(
                    AuthAction.Delete,
                    PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                    accountId
                ),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.DeleteAccountAsync(accountId), Times.Once);
    }

    [Fact]
    public async Task DeleteAccountAsync_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var accountId = 5;
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Delete,
                    PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                    accountId
                )
            )
            .ReturnsAsync(false);

        var result = await _decorator.DeleteAccountAsync(accountId);

        Assert.Equal(DeleteAccountResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(x => x.DeleteAccountAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AddUserToAccountAsync_CallsAuthorizationProvider()
    {
        var request = new AddUserToAccountRequest(1, 5);
        var expectedResult = new AddUserToAccountResult(AddUserToAccountResultCode.Success, "");
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.ACCOUNT_RESOURCE_TYPE, 5)
            )
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.AddUserToAccountAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.AddUserToAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(
                    AuthAction.Update,
                    PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                    5
                ),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.AddUserToAccountAsync(request), Times.Once);
    }

    [Fact]
    public async Task AddUserToAccountAsync_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var request = new AddUserToAccountRequest(1, 5);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.ACCOUNT_RESOURCE_TYPE, 5)
            )
            .ReturnsAsync(false);

        var result = await _decorator.AddUserToAccountAsync(request);

        Assert.Equal(AddUserToAccountResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.AddUserToAccountAsync(It.IsAny<AddUserToAccountRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RemoveUserFromAccountAsync_CallsAuthorizationProvider()
    {
        var request = new RemoveUserFromAccountRequest(1, 5);
        var expectedResult = new RemoveUserFromAccountResult(
            RemoveUserFromAccountResultCode.Success,
            ""
        );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.ACCOUNT_RESOURCE_TYPE, 5)
            )
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.RemoveUserFromAccountAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RemoveUserFromAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(
                    AuthAction.Update,
                    PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                    5
                ),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.RemoveUserFromAccountAsync(request), Times.Once);
    }

    [Fact]
    public async Task RemoveUserFromAccountAsync_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var request = new RemoveUserFromAccountRequest(1, 5);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.ACCOUNT_RESOURCE_TYPE, 5)
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
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AccountProcessorAuthorizationDecorator(null!, _mockAuthorizationProvider.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullAuthorizationProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new AccountProcessorAuthorizationDecorator(_mockInnerProcessor.Object, null!)
        );
}
