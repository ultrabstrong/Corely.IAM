using System.Text.Json;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Processors;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Corely.IAM.UnitTests.Security.Processors;

public class AccountKeyProcessorTests
{
    private const string PASSWORD = "current-password";
    private const string TOTP = "123456";

    private readonly ServiceFactory _serviceFactory = new();
    private readonly ISecurityProvider _securityProvider;
    private readonly Mock<IReauthenticationProvider> _mockReauthentication = new();
    private readonly AccountKeyProcessor _processor;
    private readonly Guid _userId = Guid.CreateVersion7();

    public AccountKeyProcessorTests()
    {
        _securityProvider = _serviceFactory.GetRequiredService<ISecurityProvider>();
        _mockReauthentication
            .Setup(x => x.VerifyCurrentPasswordAsync(It.IsAny<Guid>(), It.IsAny<string?>()))
            .ReturnsAsync(ReauthenticationResult.Succeeded);
        _mockReauthentication
            .Setup(x => x.VerifyTotpCodeAsync(It.IsAny<Guid>(), It.IsAny<string?>()))
            .ReturnsAsync(ReauthenticationResult.Succeeded);
        _processor = new AccountKeyProcessor(
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountEntity>>(),
            _securityProvider,
            _mockReauthentication.Object,
            _serviceFactory.GetRequiredService<IUserContextProvider>(),
            _serviceFactory.GetRequiredService<ILogger<AccountKeyProcessor>>()
        );
        _serviceFactory
            .GetRequiredService<IUserContextSetter>()
            .SetUserContext(
                new UserContext(
                    new User
                    {
                        Id = _userId,
                        Username = "owner",
                        Email = "owner@test.com",
                    },
                    new Account { Id = Guid.CreateVersion7(), AccountName = "TestAccount" },
                    "test-device",
                    []
                )
            );
    }

    private async Task<Guid> CreateAccountAsync()
    {
        var accountId = Guid.CreateVersion7();
        await _serviceFactory
            .GetRequiredService<IRepo<AccountEntity>>()
            .CreateAsync(
                new AccountEntity
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
                }
            );
        return accountId;
    }

    private async Task AddVersionAsync(Guid accountId, AsymmetricKey key, int version)
    {
        key.Version = version;
        await _serviceFactory
            .GetRequiredService<IRepo<AccountAsymmetricKeyEntity>>()
            .CreateAsync(key.ToAccountEntity(accountId));
        var account = await _serviceFactory
            .GetRequiredService<IRepo<AccountEntity>>()
            .GetAsync(a => a.Id == accountId);
        account!.AsymmetricKeys!.Add(key.ToAccountEntity(accountId));
    }

    private static RevealAccountKeyRequest Request(Guid accountId, KeyType keyType) =>
        new(accountId, keyType, PASSWORD, TOTP);

    [Fact]
    public async Task GetJsonWebKeySet_ListsTheSigningAndEncryptionKeys_ForANewAccount()
    {
        var accountId = await CreateAccountAsync();

        var result = await _processor.GetJsonWebKeySetAsync(accountId);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        var signing = result.Data!.Keys.Single(k => k.Use == AccountJsonWebKey.USE_SIGNATURE);
        Assert.Equal(AccountJsonWebKey.KEY_TYPE_ELLIPTIC_CURVE, signing.KeyType);
        Assert.Equal("ES256", signing.Algorithm);
        Assert.Equal("P-256", signing.Curve);
        Assert.Equal(32, Base64UrlEncoder.DecodeBytes(signing.X).Length);
        Assert.Equal(32, Base64UrlEncoder.DecodeBytes(signing.Y).Length);
        Assert.Equal(
            AccountJsonWebKey.KeyIdFor(accountId, AccountJsonWebKey.USE_SIGNATURE, 1),
            signing.KeyId
        );

        var encryption = result.Data.Keys.Single(k => k.Use == AccountJsonWebKey.USE_ENCRYPTION);
        Assert.Equal(AccountJsonWebKey.KEY_TYPE_RSA, encryption.KeyType);
        Assert.Equal("RSA-OAEP-256", encryption.Algorithm);
        Assert.Equal(256, Base64UrlEncoder.DecodeBytes(encryption.Modulus).Length);
        Assert.Equal("AQAB", encryption.Exponent);
    }

    [Fact]
    public async Task GetJsonWebKeySet_ReturnsNotFound_ForAMissingAccount()
    {
        var result = await _processor.GetJsonWebKeySetAsync(Guid.CreateVersion7());

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetJsonWebKeySet_WritesNoPrivateField_ForAnyKey()
    {
        var accountId = await CreateAccountAsync();

        var result = await _processor.GetJsonWebKeySetAsync(accountId);

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result.Data));
        var written = json
            .RootElement.GetProperty("keys")
            .EnumerateArray()
            .SelectMany(k => k.EnumerateObject().Select(p => p.Name))
            .ToHashSet();
        Assert.Contains("kid", written);
        foreach (var privateField in new[] { "d", "p", "q", "dp", "dq", "qi", "k" })
            Assert.DoesNotContain(privateField, written);
    }

    [Fact]
    public void AccountJsonWebKey_HasNoPropertyForAPrivateField()
    {
        var names = typeof(AccountJsonWebKey)
            .GetProperties()
            .Select(p => p.Name.ToLowerInvariant())
            .ToHashSet();

        foreach (var privateField in new[] { "d", "p", "q", "dp", "dq", "qi", "k" })
            Assert.DoesNotContain(privateField, names);
    }

    [Fact]
    public async Task GetJsonWebKeySet_KeepsEverySigningVersionNewestFirst_AndOnlyTheCurrentEncryptionKey()
    {
        var accountId = await CreateAccountAsync();
        await AddVersionAsync(
            accountId,
            _securityProvider.GetAsymmetricSignatureKeyEncryptedWithSystemKey(),
            2
        );
        await AddVersionAsync(
            accountId,
            _securityProvider.GetAsymmetricEncryptionKeyEncryptedWithSystemKey(),
            2
        );

        var result = await _processor.GetJsonWebKeySetAsync(accountId);

        Assert.Equal(
            [
                AccountJsonWebKey.KeyIdFor(accountId, AccountJsonWebKey.USE_SIGNATURE, 2),
                AccountJsonWebKey.KeyIdFor(accountId, AccountJsonWebKey.USE_SIGNATURE, 1),
            ],
            result
                .Data!.Keys.Where(k => k.Use == AccountJsonWebKey.USE_SIGNATURE)
                .Select(k => k.KeyId)
        );
        Assert.Equal(
            [AccountJsonWebKey.KeyIdFor(accountId, AccountJsonWebKey.USE_ENCRYPTION, 2)],
            result
                .Data.Keys.Where(k => k.Use == AccountJsonWebKey.USE_ENCRYPTION)
                .Select(k => k.KeyId)
        );
    }

    [Theory]
    [InlineData(KeyType.SymmetricEncryption)]
    [InlineData(KeyType.AsymmetricEncryption)]
    [InlineData(KeyType.AsymmetricSignature)]
    public async Task RevealKey_ReturnsTheCurrentVersionDecrypted_AfterReauthentication(
        KeyType keyType
    )
    {
        var accountId = await CreateAccountAsync();

        var result = await _processor.RevealKeyAsync(Request(accountId, keyType));

        Assert.Equal(RevealAccountKeyResultCode.Success, result.ResultCode);
        Assert.Equal(1, result.Version);
        Assert.False(string.IsNullOrWhiteSpace(result.ProviderName));
        Assert.NotEmpty(Convert.FromBase64String(result.ReadKey()!));
        _mockReauthentication.Verify(
            x => x.VerifyCurrentPasswordAsync(_userId, PASSWORD),
            Times.Once
        );
        _mockReauthentication.Verify(x => x.VerifyTotpCodeAsync(_userId, TOTP), Times.Once);
    }

    [Theory]
    [InlineData(
        ReauthenticationResultCode.InvalidCurrentPasswordError,
        RevealAccountKeyResultCode.InvalidCurrentPasswordError
    )]
    [InlineData(
        ReauthenticationResultCode.UserLockedError,
        RevealAccountKeyResultCode.UserLockedError
    )]
    [InlineData(
        ReauthenticationResultCode.BasicAuthNotFoundError,
        RevealAccountKeyResultCode.BasicAuthNotFoundError
    )]
    public async Task RevealKey_RevealsNothing_ForAFailedPassword(
        Enum reauthentication,
        RevealAccountKeyResultCode expected
    )
    {
        var accountId = await CreateAccountAsync();
        _mockReauthentication
            .Setup(x => x.VerifyCurrentPasswordAsync(It.IsAny<Guid>(), It.IsAny<string?>()))
            .ReturnsAsync(
                new ReauthenticationResult((ReauthenticationResultCode)reauthentication, "no")
            );

        var result = await _processor.RevealKeyAsync(
            Request(accountId, KeyType.SymmetricEncryption)
        );

        Assert.Equal(expected, result.ResultCode);
        Assert.Null(result.ReadKey());
        _mockReauthentication.Verify(
            x => x.VerifyTotpCodeAsync(It.IsAny<Guid>(), It.IsAny<string?>()),
            Times.Never
        );
    }

    [Theory]
    [InlineData(
        ReauthenticationResultCode.TotpRequiredError,
        RevealAccountKeyResultCode.TotpRequiredError
    )]
    [InlineData(
        ReauthenticationResultCode.InvalidTotpCodeError,
        RevealAccountKeyResultCode.InvalidTotpCodeError
    )]
    public async Task RevealKey_RevealsNothing_ForAFailedSecondFactor(
        Enum reauthentication,
        RevealAccountKeyResultCode expected
    )
    {
        var accountId = await CreateAccountAsync();
        _mockReauthentication
            .Setup(x => x.VerifyTotpCodeAsync(It.IsAny<Guid>(), It.IsAny<string?>()))
            .ReturnsAsync(
                new ReauthenticationResult((ReauthenticationResultCode)reauthentication, "no")
            );

        var result = await _processor.RevealKeyAsync(
            Request(accountId, KeyType.AsymmetricSignature)
        );

        Assert.Equal(expected, result.ResultCode);
        Assert.Null(result.ReadKey());
    }

    [Fact]
    public async Task RevealKey_ReturnsNotFound_ForAMissingAccount()
    {
        var result = await _processor.RevealKeyAsync(
            Request(Guid.CreateVersion7(), KeyType.SymmetricEncryption)
        );

        Assert.Equal(RevealAccountKeyResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.ReadKey());
    }

    [Fact]
    public void RevealAccountKeyResult_KeepsTheKeyOutOfSerializationAndToString()
    {
        var result = new RevealAccountKeyResult(
            RevealAccountKeyResultCode.Success,
            string.Empty,
            "the-secret-key",
            "AES",
            2
        );

        Assert.DoesNotContain("the-secret-key", JsonSerializer.Serialize(result));
        Assert.DoesNotContain("the-secret-key", result.ToString()!);
        Assert.Equal("the-secret-key", result.ReadKey());
    }

    [Fact]
    public void RevealAccountKeyRequest_KeepsTheSecretsOutOfSerializationAndToString()
    {
        var request = new RevealAccountKeyRequest(
            Guid.CreateVersion7(),
            KeyType.SymmetricEncryption,
            "the-password",
            "the-code"
        );

        var written = JsonSerializer.Serialize(request) + request;

        Assert.DoesNotContain("the-password", written);
        Assert.DoesNotContain("the-code", written);
    }
}
