using Corely.IAM.Accounts.Models;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Processors;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.UnitTests.Security.Processors;

public class AccountKeyProcessorAuthorizationDecoratorTests
{
    private readonly Mock<IAccountKeyProcessor> _mockInnerProcessor = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly Mock<IUserContextProvider> _mockUserContextProvider = new();
    private readonly AccountKeyProcessorAuthorizationDecorator _decorator;
    private readonly Guid _accountId = Guid.CreateVersion7();
    private readonly RevealAccountKeyRequest _request;
    private readonly RevealAccountKeyResult _success = new(
        RevealAccountKeyResultCode.Success,
        string.Empty,
        "key"
    );

    public AccountKeyProcessorAuthorizationDecoratorTests()
    {
        _request = new RevealAccountKeyRequest(
            _accountId,
            KeyType.AsymmetricSignature,
            "password",
            null
        );
        _mockInnerProcessor
            .Setup(x => x.RevealKeyAsync(It.IsAny<RevealAccountKeyRequest>()))
            .ReturnsAsync(_success);
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(true);
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext(_accountId)).Returns(true);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Execute,
                    Account.SECRET_KEYS_RESOURCE_TYPE,
                    _accountId
                )
            )
            .ReturnsAsync(true);
        SignInUser(enteredAsPlatformMember: false);
        _decorator = new AccountKeyProcessorAuthorizationDecorator(
            _mockInnerProcessor.Object,
            _mockAuthorizationProvider.Object,
            _mockUserContextProvider.Object
        );
    }

    private void SignInUser(bool enteredAsPlatformMember) =>
        _mockUserContextProvider
            .Setup(x => x.GetUserContext())
            .Returns(
                new UserContext(
                    new User { Id = Guid.CreateVersion7(), Username = "owner" },
                    new Account { Id = _accountId, AccountName = "account" },
                    "device",
                    [],
                    enteredAsPlatformMember: enteredAsPlatformMember
                )
            );

    private async Task AssertRefusedAsync()
    {
        var result = await _decorator.RevealKeyAsync(_request);

        Assert.Equal(RevealAccountKeyResultCode.UnauthorizedError, result.ResultCode);
        Assert.Null(result.ReadKey());
        _mockInnerProcessor.Verify(
            x => x.RevealKeyAsync(It.IsAny<RevealAccountKeyRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RevealKey_DelegatesToInner_ForAMemberHoldingThePermission()
    {
        var result = await _decorator.RevealKeyAsync(_request);

        Assert.Same(_success, result);
    }

    [Fact]
    public async Task RevealKey_Refuses_ForSystemContextOrAnApiKey()
    {
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(false);

        await AssertRefusedAsync();
    }

    [Fact]
    public async Task RevealKey_Refuses_ForAnotherAccount()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext(_accountId)).Returns(false);

        await AssertRefusedAsync();
    }

    [Fact]
    public async Task RevealKey_Refuses_ForAPlatformMemberWhoEnteredTheAccount()
    {
        SignInUser(enteredAsPlatformMember: true);

        await AssertRefusedAsync();
    }

    [Fact]
    public async Task RevealKey_Refuses_WithoutThePermission()
    {
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Execute,
                    Account.SECRET_KEYS_RESOURCE_TYPE,
                    _accountId
                )
            )
            .ReturnsAsync(false);

        await AssertRefusedAsync();
    }

    [Fact]
    public async Task RevealKey_Refuses_ForAKeysContextHoldingThePermission()
    {
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(false);
        _mockUserContextProvider
            .Setup(x => x.GetUserContext())
            .Returns(
                new UserContext(
                    new ApiKey { Id = Guid.CreateVersion7() },
                    new Account { Id = _accountId, AccountName = "account" },
                    "device"
                )
            );

        await AssertRefusedAsync();
    }

    [Fact]
    public async Task GetJsonWebKeySet_AsksForNoPermission_BecauseTheKeysArePublic()
    {
        var accountId = Guid.CreateVersion7();
        var keys = new GetResult<AccountJsonWebKeySet>(
            RetrieveResultCode.Success,
            string.Empty,
            new AccountJsonWebKeySet([])
        );
        _mockInnerProcessor.Setup(x => x.GetJsonWebKeySetAsync(accountId)).ReturnsAsync(keys);

        var result = await _decorator.GetJsonWebKeySetAsync(accountId);

        Assert.Same(keys, result);
        _mockAuthorizationProvider.VerifyNoOtherCalls();
    }
}
