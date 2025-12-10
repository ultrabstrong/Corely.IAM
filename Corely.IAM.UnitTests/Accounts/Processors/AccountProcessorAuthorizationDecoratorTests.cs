using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Exceptions;
using Corely.IAM.Security.Processors;

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
        var expectedResult = new CreateAccountResult(CreateAccountResultCode.Success, "", 1);
        _mockInnerProcessor.Setup(x => x.CreateAccountAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.CreateAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.AuthorizeAsync(It.IsAny<string>(), It.IsAny<AuthAction>(), It.IsAny<int?>()),
            Times.Never
        );
        _mockInnerProcessor.Verify(x => x.CreateAccountAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetAccountAsyncById_CallsAuthorizationProviderWithResourceId()
    {
        var accountId = 5;
        var expectedAccount = new Account { Id = accountId, AccountName = "TestAccount" };
        _mockInnerProcessor.Setup(x => x.GetAccountAsync(accountId)).ReturnsAsync(expectedAccount);

        var result = await _decorator.GetAccountAsync(accountId);

        Assert.Equal(expectedAccount, result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.AuthorizeAsync(
                    PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                    AuthAction.Read,
                    accountId
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetAccountAsyncById_ThrowsAuthorizationException_WhenNotAuthorized()
    {
        var accountId = 5;
        _mockAuthorizationProvider
            .Setup(x =>
                x.AuthorizeAsync(
                    PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                    AuthAction.Read,
                    accountId
                )
            )
            .ThrowsAsync(
                new AuthorizationException(
                    PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                    AuthAction.Read.ToString(),
                    accountId
                )
            );

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _decorator.GetAccountAsync(accountId)
        );

        _mockInnerProcessor.Verify(x => x.GetAccountAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetAccountAsyncByName_CallsAuthorizationProvider()
    {
        var accountName = "TestAccount";
        var expectedAccount = new Account { Id = 1, AccountName = accountName };
        _mockInnerProcessor
            .Setup(x => x.GetAccountAsync(accountName))
            .ReturnsAsync(expectedAccount);

        var result = await _decorator.GetAccountAsync(accountName);

        Assert.Equal(expectedAccount, result);
        _mockAuthorizationProvider.Verify(
            x => x.AuthorizeAsync(PermissionConstants.ACCOUNT_RESOURCE_TYPE, AuthAction.Read, null),
            Times.Once
        );
    }

    [Fact]
    public async Task GetAccountAsyncByName_ThrowsAuthorizationException_WhenNotAuthorized()
    {
        var accountName = "TestAccount";
        _mockAuthorizationProvider
            .Setup(x =>
                x.AuthorizeAsync(PermissionConstants.ACCOUNT_RESOURCE_TYPE, AuthAction.Read, null)
            )
            .ThrowsAsync(
                new AuthorizationException(
                    PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                    AuthAction.Read.ToString()
                )
            );

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _decorator.GetAccountAsync(accountName)
        );

        _mockInnerProcessor.Verify(x => x.GetAccountAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAccountAsync_CallsAuthorizationProvider()
    {
        var accountId = 5;
        var expectedResult = new DeleteAccountResult(DeleteAccountResultCode.Success, "");
        _mockInnerProcessor
            .Setup(x => x.DeleteAccountAsync(accountId))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.DeleteAccountAsync(accountId);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.AuthorizeAsync(
                    PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                    AuthAction.Delete,
                    accountId
                ),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.DeleteAccountAsync(accountId), Times.Once);
    }

    [Fact]
    public async Task DeleteAccountAsync_ThrowsAuthorizationException_WhenNotAuthorized()
    {
        var accountId = 5;
        _mockAuthorizationProvider
            .Setup(x =>
                x.AuthorizeAsync(
                    PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                    AuthAction.Delete,
                    accountId
                )
            )
            .ThrowsAsync(
                new AuthorizationException(
                    PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                    AuthAction.Delete.ToString(),
                    accountId
                )
            );

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _decorator.DeleteAccountAsync(accountId)
        );

        _mockInnerProcessor.Verify(x => x.DeleteAccountAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AddUserToAccountAsync_CallsAuthorizationProvider()
    {
        var request = new AddUserToAccountRequest(1, 5);
        var expectedResult = new AddUserToAccountResult(AddUserToAccountResultCode.Success, "");
        _mockInnerProcessor
            .Setup(x => x.AddUserToAccountAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.AddUserToAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.AuthorizeAsync(PermissionConstants.ACCOUNT_RESOURCE_TYPE, AuthAction.Update, 5),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.AddUserToAccountAsync(request), Times.Once);
    }

    [Fact]
    public async Task AddUserToAccountAsync_ThrowsAuthorizationException_WhenNotAuthorized()
    {
        var request = new AddUserToAccountRequest(1, 5);
        _mockAuthorizationProvider
            .Setup(x =>
                x.AuthorizeAsync(PermissionConstants.ACCOUNT_RESOURCE_TYPE, AuthAction.Update, 5)
            )
            .ThrowsAsync(
                new AuthorizationException(
                    PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                    AuthAction.Update.ToString(),
                    5
                )
            );

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _decorator.AddUserToAccountAsync(request)
        );

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
        _mockInnerProcessor
            .Setup(x => x.RemoveUserFromAccountAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RemoveUserFromAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.AuthorizeAsync(PermissionConstants.ACCOUNT_RESOURCE_TYPE, AuthAction.Update, 5),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.RemoveUserFromAccountAsync(request), Times.Once);
    }

    [Fact]
    public async Task RemoveUserFromAccountAsync_ThrowsAuthorizationException_WhenNotAuthorized()
    {
        var request = new RemoveUserFromAccountRequest(1, 5);
        _mockAuthorizationProvider
            .Setup(x =>
                x.AuthorizeAsync(PermissionConstants.ACCOUNT_RESOURCE_TYPE, AuthAction.Update, 5)
            )
            .ThrowsAsync(
                new AuthorizationException(
                    PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                    AuthAction.Update.ToString(),
                    5
                )
            );

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _decorator.RemoveUserFromAccountAsync(request)
        );

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
