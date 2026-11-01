using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Processors;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.UnitTests.Security.Processors;

public class KeyRotationProcessorAuthorizationDecoratorTests
{
    private readonly Mock<IKeyRotationProcessor> _mockInnerProcessor = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly KeyRotationProcessorAuthorizationDecorator _decorator;
    private readonly ModifyResult _success = new(ModifyResultCode.Success, string.Empty);

    public KeyRotationProcessorAuthorizationDecoratorTests()
    {
        _mockInnerProcessor
            .Setup(x => x.RotateAccountKeyAsync(It.IsAny<RotateAccountKeyRequest>()))
            .ReturnsAsync(_success);
        _mockInnerProcessor
            .Setup(x => x.RotateCurrentUserKeyAsync(It.IsAny<KeyType>()))
            .ReturnsAsync(_success);
        _decorator = new KeyRotationProcessorAuthorizationDecorator(
            _mockInnerProcessor.Object,
            _mockAuthorizationProvider.Object
        );
    }

    [Fact]
    public async Task RotateAccountKey_DelegatesToInner_ForAccountUpdatePermission()
    {
        var request = new RotateAccountKeyRequest(
            Guid.CreateVersion7(),
            KeyType.SymmetricEncryption
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext(request.AccountId)).Returns(true);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, Account.RESOURCE_TYPE, request.AccountId)
            )
            .ReturnsAsync(true);

        var result = await _decorator.RotateAccountKeyAsync(request);

        Assert.Same(_success, result);
    }

    [Fact]
    public async Task RotateAccountKey_ReturnsUnauthorized_WithoutUpdatePermission()
    {
        var request = new RotateAccountKeyRequest(
            Guid.CreateVersion7(),
            KeyType.SymmetricEncryption
        );
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext(request.AccountId)).Returns(true);

        var result = await _decorator.RotateAccountKeyAsync(request);

        Assert.Equal(ModifyResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.RotateAccountKeyAsync(It.IsAny<RotateAccountKeyRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RotateAccountKey_ReturnsUnauthorized_WithoutAccountContext()
    {
        var request = new RotateAccountKeyRequest(
            Guid.CreateVersion7(),
            KeyType.SymmetricEncryption
        );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<Guid[]>())
            )
            .ReturnsAsync(true);

        var result = await _decorator.RotateAccountKeyAsync(request);

        Assert.Equal(ModifyResultCode.UnauthorizedError, result.ResultCode);
    }

    [Fact]
    public async Task RotateCurrentUserKey_DelegatesToInner_ForNonSystemUser()
    {
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(true);

        var result = await _decorator.RotateCurrentUserKeyAsync(KeyType.AsymmetricSignature);

        Assert.Same(_success, result);
    }

    [Fact]
    public async Task RotateCurrentUserKey_ReturnsUnauthorized_ForSystemContext()
    {
        var result = await _decorator.RotateCurrentUserKeyAsync(KeyType.AsymmetricSignature);

        Assert.Equal(ModifyResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.RotateCurrentUserKeyAsync(It.IsAny<KeyType>()),
            Times.Never
        );
    }

    private void AllowCompromising(Guid accountId)
    {
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(true);
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext(accountId)).Returns(true);
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedAsync(AuthAction.Update, Account.RESOURCE_TYPE, accountId))
            .ReturnsAsync(true);
    }

    [Fact]
    public async Task CompromiseCalls_DelegateToInner_ForAUserWhoCanUpdateTheAccount()
    {
        var accountId = Guid.CreateVersion7();
        AllowCompromising(accountId);
        var proof = new AccountKeyProof("password", null);
        var success = new CompromiseAccountKeyResult(
            CompromiseAccountKeyResultCode.Success,
            string.Empty
        );
        _mockInnerProcessor
            .Setup(x =>
                x.MarkAccountKeyCompromisedAsync(It.IsAny<MarkAccountKeyCompromisedRequest>())
            )
            .ReturnsAsync(success);
        _mockInnerProcessor
            .Setup(x =>
                x.RotateCompromisedAccountKeyAsync(It.IsAny<RotateCompromisedAccountKeyRequest>())
            )
            .ReturnsAsync(success);

        Assert.Same(
            success,
            await _decorator.MarkAccountKeyCompromisedAsync(
                new MarkAccountKeyCompromisedRequest(
                    accountId,
                    KeyType.SymmetricEncryption,
                    1,
                    proof
                )
            )
        );
        Assert.Same(
            success,
            await _decorator.RotateCompromisedAccountKeyAsync(
                new RotateCompromisedAccountKeyRequest(
                    accountId,
                    KeyType.SymmetricEncryption,
                    proof
                )
            )
        );
    }

    public static TheoryData<string> Denials => ["system or api key", "other account", "no update"];

    [Theory]
    [MemberData(nameof(Denials))]
    public async Task CompromiseCalls_Refuse_WithoutAUserWhoCanUpdateTheAccount(string denial)
    {
        var accountId = Guid.CreateVersion7();
        AllowCompromising(accountId);
        if (denial == "system or api key")
            _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(false);
        if (denial == "other account")
            _mockAuthorizationProvider.Setup(x => x.HasAccountContext(accountId)).Returns(false);
        if (denial == "no update")
            _mockAuthorizationProvider
                .Setup(x =>
                    x.IsAuthorizedAsync(AuthAction.Update, Account.RESOURCE_TYPE, accountId)
                )
                .ReturnsAsync(false);
        var proof = new AccountKeyProof("password", null);

        var marked = await _decorator.MarkAccountKeyCompromisedAsync(
            new MarkAccountKeyCompromisedRequest(accountId, KeyType.SymmetricEncryption, 1, proof)
        );
        var rotated = await _decorator.RotateCompromisedAccountKeyAsync(
            new RotateCompromisedAccountKeyRequest(accountId, KeyType.SymmetricEncryption, proof)
        );

        Assert.Equal(CompromiseAccountKeyResultCode.UnauthorizedError, marked.ResultCode);
        Assert.Equal(CompromiseAccountKeyResultCode.UnauthorizedError, rotated.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.MarkAccountKeyCompromisedAsync(It.IsAny<MarkAccountKeyCompromisedRequest>()),
            Times.Never
        );
        _mockInnerProcessor.Verify(
            x => x.RotateCompromisedAccountKeyAsync(It.IsAny<RotateCompromisedAccountKeyRequest>()),
            Times.Never
        );
    }
}
