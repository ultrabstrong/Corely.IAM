using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Processors;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Security.Processors;

public class KeyRotationProcessorCompromiseTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly ISecurityProvider _securityProvider;
    private readonly Mock<IReauthenticationProvider> _mockReauthentication = new();
    private readonly Mock<IUnitOfWorkProvider> _mockUowProvider = new();
    private readonly KeyRotationProcessor _processor;
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Guid _accountId = Guid.CreateVersion7();
    private readonly AccountKeyProof _proof = new("password", "123456");

    public KeyRotationProcessorCompromiseTests()
    {
        _securityProvider = _serviceFactory.GetRequiredService<ISecurityProvider>();
        _mockReauthentication
            .Setup(x => x.VerifyAccountKeyProofAsync(It.IsAny<Guid>(), It.IsAny<AccountKeyProof>()))
            .ReturnsAsync(ReauthenticationResult.Succeeded);
        _processor = new KeyRotationProcessor(
            _serviceFactory.GetRequiredService<IReadonlyRepo<UserEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<UserSymmetricKeyEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<UserAsymmetricKeyEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<AccountSymmetricKeyEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<AccountAsymmetricKeyEntity>>(),
            _securityProvider,
            _serviceFactory.GetRequiredService<IUserContextProvider>(),
            _serviceFactory.GetRequiredService<ILogger<KeyRotationProcessor>>(),
            _serviceFactory.GetRequiredService<TimeProvider>(),
            _mockReauthentication.Object,
            _mockUowProvider.Object
        );
        _serviceFactory
            .GetRequiredService<IUserContextSetter>()
            .SetUserContext(
                new UserContext(
                    new User { Id = _userId, Username = "owner" },
                    new Account { Id = _accountId, AccountName = "keys" },
                    "test-device",
                    []
                )
            );
    }

    private async Task CreateAccountWithVersionsAsync(
        int versions,
        DateTime? firstCompromisedUtc = null
    )
    {
        var symmetric = new List<AccountSymmetricKeyEntity>();
        var asymmetric = new List<AccountAsymmetricKeyEntity>();
        for (var version = 1; version <= versions; version++)
        {
            var compromisedUtc = version == 1 ? firstCompromisedUtc : null;
            var symmetricKey = _securityProvider.GetSymmetricEncryptionKeyEncryptedWithSystemKey();
            symmetricKey.Version = version;
            var symmetricEntity = symmetricKey.ToAccountEntity(_accountId);
            symmetricEntity.CompromisedUtc = compromisedUtc;
            symmetric.Add(symmetricEntity);

            var encryptionKey =
                _securityProvider.GetAsymmetricEncryptionKeyEncryptedWithSystemKey();
            encryptionKey.Version = version;
            var encryptionEntity = encryptionKey.ToAccountEntity(_accountId);
            encryptionEntity.CompromisedUtc = compromisedUtc;
            asymmetric.Add(encryptionEntity);

            var signatureKey = _securityProvider.GetAsymmetricSignatureKeyEncryptedWithSystemKey();
            signatureKey.Version = version;
            var signatureEntity = signatureKey.ToAccountEntity(_accountId);
            signatureEntity.CompromisedUtc = compromisedUtc;
            asymmetric.Add(signatureEntity);
        }

        foreach (var key in symmetric)
            await _serviceFactory
                .GetRequiredService<IRepo<AccountSymmetricKeyEntity>>()
                .CreateAsync(key);
        foreach (var key in asymmetric)
            await _serviceFactory
                .GetRequiredService<IRepo<AccountAsymmetricKeyEntity>>()
                .CreateAsync(key);
        await _serviceFactory
            .GetRequiredService<IRepo<AccountEntity>>()
            .CreateAsync(
                new AccountEntity
                {
                    Id = _accountId,
                    AccountName = "keys",
                    SymmetricKeys = symmetric,
                    AsymmetricKeys = asymmetric,
                }
            );
    }

    private async Task<DateTime?> CompromisedUtcOfAsync(KeyType keyType, int version)
    {
        if (keyType == KeyType.SymmetricEncryption)
        {
            var symmetric = await _serviceFactory
                .GetRequiredService<IRepo<AccountSymmetricKeyEntity>>()
                .GetAsync(k => k.AccountId == _accountId && k.Version == version);
            return symmetric!.CompromisedUtc;
        }

        var keyUsedFor =
            keyType == KeyType.AsymmetricSignature ? KeyUsedFor.Signature : KeyUsedFor.Encryption;
        var asymmetric = await _serviceFactory
            .GetRequiredService<IRepo<AccountAsymmetricKeyEntity>>()
            .GetAsync(k =>
                k.AccountId == _accountId && k.Version == version && k.KeyUsedFor == keyUsedFor
            );
        return asymmetric!.CompromisedUtc;
    }

    private void RefuseProof() =>
        _mockReauthentication
            .Setup(x => x.VerifyAccountKeyProofAsync(It.IsAny<Guid>(), It.IsAny<AccountKeyProof>()))
            .ReturnsAsync(
                new ReauthenticationResult(
                    ReauthenticationResultCode.InvalidCurrentPasswordError,
                    "Current password is incorrect"
                )
            );

    private MarkAccountKeyCompromisedRequest Mark(KeyType keyType, int version) =>
        new(_accountId, keyType, version, _proof);

    [Theory]
    [InlineData(KeyType.SymmetricEncryption)]
    [InlineData(KeyType.AsymmetricEncryption)]
    [InlineData(KeyType.AsymmetricSignature)]
    public async Task MarkAccountKeyCompromised_MarksTheOlderVersionOnly_ForEveryKeyType(
        KeyType keyType
    )
    {
        await CreateAccountWithVersionsAsync(3);

        var result = await _processor.MarkAccountKeyCompromisedAsync(Mark(keyType, 2));

        Assert.Equal(CompromiseAccountKeyResultCode.Success, result.ResultCode);
        Assert.NotNull(await CompromisedUtcOfAsync(keyType, 2));
        Assert.Null(await CompromisedUtcOfAsync(keyType, 1));
        Assert.Null(await CompromisedUtcOfAsync(keyType, 3));
        _mockReauthentication.Verify(
            x => x.VerifyAccountKeyProofAsync(_userId, _proof),
            Times.Once
        );
    }

    [Theory]
    [InlineData(KeyType.SymmetricEncryption)]
    [InlineData(KeyType.AsymmetricEncryption)]
    [InlineData(KeyType.AsymmetricSignature)]
    public async Task MarkAccountKeyCompromised_RefusesTheCurrentVersion_ForEveryKeyType(
        KeyType keyType
    )
    {
        await CreateAccountWithVersionsAsync(2);

        var result = await _processor.MarkAccountKeyCompromisedAsync(Mark(keyType, 2));

        Assert.Equal(CompromiseAccountKeyResultCode.CurrentVersionError, result.ResultCode);
        Assert.Null(await CompromisedUtcOfAsync(keyType, 2));
    }

    [Fact]
    public async Task MarkAccountKeyCompromised_RefusesTheOnlyVersion()
    {
        await CreateAccountWithVersionsAsync(1);

        var result = await _processor.MarkAccountKeyCompromisedAsync(
            Mark(KeyType.AsymmetricSignature, 1)
        );

        Assert.Equal(CompromiseAccountKeyResultCode.CurrentVersionError, result.ResultCode);
    }

    [Fact]
    public async Task MarkAccountKeyCompromised_ReturnsNotFound_ForAVersionThatDoesNotExist()
    {
        await CreateAccountWithVersionsAsync(2);

        var result = await _processor.MarkAccountKeyCompromisedAsync(
            Mark(KeyType.AsymmetricSignature, 9)
        );

        Assert.Equal(CompromiseAccountKeyResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task MarkAccountKeyCompromised_ReturnsNotFound_ForAMissingAccount()
    {
        var result = await _processor.MarkAccountKeyCompromisedAsync(
            Mark(KeyType.SymmetricEncryption, 1)
        );

        Assert.Equal(CompromiseAccountKeyResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task MarkAccountKeyCompromised_LeavesAnEarlierMarkAlone_ForAnAlreadyMarkedVersion()
    {
        var firstMarkedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await CreateAccountWithVersionsAsync(2, firstMarkedUtc);

        var result = await _processor.MarkAccountKeyCompromisedAsync(
            Mark(KeyType.AsymmetricSignature, 1)
        );

        Assert.Equal(CompromiseAccountKeyResultCode.AlreadyCompromisedError, result.ResultCode);
        Assert.Equal(firstMarkedUtc, await CompromisedUtcOfAsync(KeyType.AsymmetricSignature, 1));
    }

    [Fact]
    public async Task MarkAccountKeyCompromised_WritesNothing_ForARefusedProof()
    {
        await CreateAccountWithVersionsAsync(2);
        RefuseProof();

        var result = await _processor.MarkAccountKeyCompromisedAsync(
            Mark(KeyType.AsymmetricSignature, 1)
        );

        Assert.Equal(CompromiseAccountKeyResultCode.InvalidCurrentPasswordError, result.ResultCode);
        Assert.Null(await CompromisedUtcOfAsync(KeyType.AsymmetricSignature, 1));
    }

    [Fact]
    public async Task RotateCompromised_NeverOpensATransaction_ForARefusedProof()
    {
        await CreateAccountWithVersionsAsync(1);
        RefuseProof();

        var result = await _processor.RotateCompromisedAccountKeyAsync(
            new RotateCompromisedAccountKeyRequest(_accountId, KeyType.AsymmetricSignature, _proof)
        );

        Assert.Equal(CompromiseAccountKeyResultCode.InvalidCurrentPasswordError, result.ResultCode);
        _mockUowProvider.Verify(x => x.BeginAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Null(await CompromisedUtcOfAsync(KeyType.AsymmetricSignature, 1));
    }

    [Fact]
    public async Task RotateCompromised_ChecksTheProofBeforeTheTransactionStarts()
    {
        await CreateAccountWithVersionsAsync(1);
        var order = new List<string>();
        _mockReauthentication
            .Setup(x => x.VerifyAccountKeyProofAsync(It.IsAny<Guid>(), It.IsAny<AccountKeyProof>()))
            .Callback(() => order.Add("proof"))
            .ReturnsAsync(ReauthenticationResult.Succeeded);
        _mockUowProvider
            .Setup(x => x.BeginAsync(It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("begin"))
            .Returns(Task.CompletedTask);

        var result = await _processor.RotateCompromisedAccountKeyAsync(
            new RotateCompromisedAccountKeyRequest(_accountId, KeyType.AsymmetricSignature, _proof)
        );

        Assert.Equal(CompromiseAccountKeyResultCode.Success, result.ResultCode);
        Assert.Equal(["proof", "begin"], order);
        _mockUowProvider.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(await CompromisedUtcOfAsync(KeyType.AsymmetricSignature, 1));
    }

    [Fact]
    public async Task RotateCompromised_RollsBack_ForAMissingAccount()
    {
        var result = await _processor.RotateCompromisedAccountKeyAsync(
            new RotateCompromisedAccountKeyRequest(_accountId, KeyType.AsymmetricSignature, _proof)
        );

        Assert.Equal(CompromiseAccountKeyResultCode.NotFoundError, result.ResultCode);
        _mockUowProvider.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUowProvider.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
