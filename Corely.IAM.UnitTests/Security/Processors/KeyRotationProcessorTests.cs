using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
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

public class KeyRotationProcessorTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly ISecurityProvider _securityProvider;
    private readonly KeyRotationProcessor _processor;

    public KeyRotationProcessorTests()
    {
        _securityProvider = _serviceFactory.GetRequiredService<ISecurityProvider>();
        _processor = new KeyRotationProcessor(
            _serviceFactory.GetRequiredService<IReadonlyRepo<UserEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<UserSymmetricKeyEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<UserAsymmetricKeyEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<AccountSymmetricKeyEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<AccountAsymmetricKeyEntity>>(),
            _securityProvider,
            _serviceFactory.GetRequiredService<IUserContextProvider>(),
            _serviceFactory.GetRequiredService<ILogger<KeyRotationProcessor>>()
        );
    }

    private async Task<UserEntity> CreateSignedInUserAsync()
    {
        var userId = Guid.CreateVersion7();
        var user = new UserEntity
        {
            Id = userId,
            Username = "rotator",
            Email = "rotator@test.com",
            SymmetricKeys =
            [
                _securityProvider
                    .GetSymmetricEncryptionKeyEncryptedWithSystemKey()
                    .ToUserEntity(userId),
            ],
            AsymmetricKeys =
            [
                _securityProvider
                    .GetAsymmetricEncryptionKeyEncryptedWithSystemKey()
                    .ToUserEntity(userId),
                _securityProvider
                    .GetAsymmetricSignatureKeyEncryptedWithSystemKey()
                    .ToUserEntity(userId),
            ],
        };
        await _serviceFactory.GetRequiredService<IRepo<UserEntity>>().CreateAsync(user);

        var account = new Account { Id = Guid.CreateVersion7(), AccountName = "TestAccount" };
        _serviceFactory
            .GetRequiredService<IUserContextSetter>()
            .SetUserContext(
                new UserContext(
                    new User
                    {
                        Id = userId,
                        Username = user.Username,
                        Email = user.Email,
                    },
                    account,
                    "device1",
                    [account]
                )
            );
        return user;
    }

    private async Task<AccountEntity> CreateAccountAsync()
    {
        var accountId = Guid.CreateVersion7();
        var account = new AccountEntity
        {
            Id = accountId,
            AccountName = "keys",
            SymmetricKeys =
            [
                _securityProvider
                    .GetSymmetricEncryptionKeyEncryptedWithSystemKey()
                    .ToAccountEntity(accountId),
            ],
            AsymmetricKeys =
            [
                _securityProvider
                    .GetAsymmetricEncryptionKeyEncryptedWithSystemKey()
                    .ToAccountEntity(accountId),
                _securityProvider
                    .GetAsymmetricSignatureKeyEncryptedWithSystemKey()
                    .ToAccountEntity(accountId),
            ],
        };
        return await _serviceFactory
            .GetRequiredService<IRepo<AccountEntity>>()
            .CreateAsync(account);
    }

    [Fact]
    public async Task RotateCurrentUserKey_CreatesSecondVersion_ForSymmetricEncryption()
    {
        var user = await CreateSignedInUserAsync();

        var result = await _processor.RotateCurrentUserKeyAsync(KeyType.SymmetricEncryption);

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        var created = await _serviceFactory
            .GetRequiredService<IRepo<UserSymmetricKeyEntity>>()
            .ListAsync(k => k.UserId == user.Id);
        var key = Assert.Single(created);
        Assert.Equal(KeyUsedFor.Encryption, key.KeyUsedFor);
        Assert.Equal(2, key.Version);
    }

    [Theory]
    [InlineData(KeyType.AsymmetricEncryption, KeyUsedFor.Encryption)]
    [InlineData(KeyType.AsymmetricSignature, KeyUsedFor.Signature)]
    public async Task RotateCurrentUserKey_CreatesSecondVersion_ForAsymmetricKeyType(
        KeyType keyType,
        KeyUsedFor expectedUse
    )
    {
        var user = await CreateSignedInUserAsync();

        var result = await _processor.RotateCurrentUserKeyAsync(keyType);

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        var created = await _serviceFactory
            .GetRequiredService<IRepo<UserAsymmetricKeyEntity>>()
            .ListAsync(k => k.UserId == user.Id);
        var key = Assert.Single(created);
        Assert.Equal(expectedUse, key.KeyUsedFor);
        Assert.Equal(2, key.Version);
    }

    [Fact]
    public async Task RotateCurrentUserKey_ReturnsNotFound_ForMissingUser()
    {
        var account = new Account { Id = Guid.CreateVersion7(), AccountName = "TestAccount" };
        _serviceFactory
            .GetRequiredService<IUserContextSetter>()
            .SetUserContext(
                new UserContext(
                    new User
                    {
                        Id = Guid.CreateVersion7(),
                        Username = "ghost",
                        Email = "ghost@test.com",
                    },
                    account,
                    "device1",
                    [account]
                )
            );

        var result = await _processor.RotateCurrentUserKeyAsync(KeyType.SymmetricEncryption);

        Assert.Equal(ModifyResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task RotateCurrentUserKey_ReturnsValidationError_ForUnknownKeyType()
    {
        await CreateSignedInUserAsync();

        var result = await _processor.RotateCurrentUserKeyAsync((KeyType)99);

        Assert.Equal(ModifyResultCode.ValidationError, result.ResultCode);
    }

    [Fact]
    public async Task RotateAccountKey_CreatesSecondVersion_ForSymmetricEncryption()
    {
        var account = await CreateAccountAsync();

        var result = await _processor.RotateAccountKeyAsync(
            new RotateAccountKeyRequest(account.Id, KeyType.SymmetricEncryption)
        );

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        var created = await _serviceFactory
            .GetRequiredService<IRepo<AccountSymmetricKeyEntity>>()
            .ListAsync(k => k.AccountId == account.Id);
        var key = Assert.Single(created);
        Assert.Equal(2, key.Version);
    }

    [Theory]
    [InlineData(KeyType.AsymmetricEncryption, KeyUsedFor.Encryption)]
    [InlineData(KeyType.AsymmetricSignature, KeyUsedFor.Signature)]
    public async Task RotateAccountKey_CreatesSecondVersion_ForAsymmetricKeyType(
        KeyType keyType,
        KeyUsedFor expectedUse
    )
    {
        var account = await CreateAccountAsync();

        var result = await _processor.RotateAccountKeyAsync(
            new RotateAccountKeyRequest(account.Id, keyType)
        );

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        var created = await _serviceFactory
            .GetRequiredService<IRepo<AccountAsymmetricKeyEntity>>()
            .ListAsync(k => k.AccountId == account.Id);
        var key = Assert.Single(created);
        Assert.Equal(expectedUse, key.KeyUsedFor);
        Assert.Equal(2, key.Version);
    }

    [Fact]
    public async Task RotateAccountKey_ReturnsNotFound_ForMissingAccount()
    {
        var result = await _processor.RotateAccountKeyAsync(
            new RotateAccountKeyRequest(Guid.CreateVersion7(), KeyType.SymmetricEncryption)
        );

        Assert.Equal(ModifyResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task RotateAccountKey_ReturnsValidationError_ForUnknownKeyType()
    {
        var account = await CreateAccountAsync();

        var result = await _processor.RotateAccountKeyAsync(
            new RotateAccountKeyRequest(account.Id, (KeyType)99)
        );

        Assert.Equal(ModifyResultCode.ValidationError, result.ResultCode);
    }
}
