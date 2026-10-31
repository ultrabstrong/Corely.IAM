using System.Security.Cryptography;
using System.Text;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Audits.Entities;
using Corely.IAM.DataAccess;
using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.Security.Encryption.Providers;
using Corely.Security.KeyStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Corely.IAM.IntegrationTests.Authorization;

public class AccountKeyTests : IAsyncLifetime
{
    private readonly IamScenario _scenario = new();

    public ValueTask InitializeAsync() => _scenario.InitializeAsync();

    public ValueTask DisposeAsync() => _scenario.DisposeAsync();

    private Task<RevealAccountKeyResult> RevealAsync(
        string username,
        KeyType keyType,
        string password = IamScenario.Password
    ) =>
        _scenario.ActAsAsync(
            username,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IRetrievalService>()
                    .RevealAccountKeyAsync(
                        new RevealAccountKeyRequest(_scenario.AccountId, keyType, password, null)
                    )
        );

    private Task<AccountJsonWebKeySet> GetKeySetAsync() =>
        _scenario.AsSystemAsync(async services =>
            (
                await services
                    .GetRequiredService<IRetrievalService>()
                    .GetAccountJsonWebKeySetAsync(_scenario.AccountId)
            ).Item!
        );

    private Task RotateAsync(KeyType keyType) =>
        _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            async services =>
            {
                var result = await services
                    .GetRequiredService<IModificationService>()
                    .RotateAccountKeyAsync(
                        new RotateAccountKeyRequest(_scenario.AccountId, keyType)
                    );
                Assert.Equal(ModifyResultCode.Success, result.ResultCode);
                return true;
            }
        );

    [Fact]
    public async Task RevealedSymmetricKey_DecryptsWhatTheAccountEncrypted()
    {
        var ciphertext = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            async services =>
                (
                    await services
                        .GetRequiredService<IRetrievalService>()
                        .GetAccountSymmetricEncryptionProviderAsync(_scenario.AccountId)
                ).Item!.Encrypt("account data")
        );

        var revealed = await RevealAsync(_scenario.OwnerUsername, KeyType.SymmetricEncryption);

        Assert.Equal(RevealAccountKeyResultCode.Success, revealed.ResultCode);
        var plaintext = new AesGcmEncryptionProvider().Decrypt(
            ciphertext,
            new InMemorySymmetricKeyStoreProvider(revealed.ReadKey()!)
        );
        Assert.Equal("account data", plaintext);
    }

    [Fact]
    public async Task RevealedEncryptionKey_ReadsWhatAStandardClientEncryptsToTheKeySet()
    {
        var keySet = await GetKeySetAsync();
        var jwk = keySet.Keys.Single(k => k.Use == AccountJsonWebKey.USE_ENCRYPTION);
        using var sender = RSA.Create(
            new RSAParameters
            {
                Modulus = Base64UrlEncoder.DecodeBytes(jwk.Modulus),
                Exponent = Base64UrlEncoder.DecodeBytes(jwk.Exponent),
            }
        );
        var ciphertext = sender.Encrypt(
            Encoding.UTF8.GetBytes("for the account"),
            RSAEncryptionPadding.OaepSHA256
        );

        var revealed = await RevealAsync(_scenario.OwnerUsername, KeyType.AsymmetricEncryption);

        using var receiver = RSA.Create();
        receiver.ImportPkcs8PrivateKey(Convert.FromBase64String(revealed.ReadKey()!), out _);
        var plaintext = receiver.Decrypt(ciphertext, RSAEncryptionPadding.OaepSHA256);
        Assert.Equal("for the account", Encoding.UTF8.GetString(plaintext));
    }

    [Fact]
    public async Task RevealedSigningKey_SignsWhatAStandardClientVerifiesFromTheKeySet()
    {
        var revealed = await RevealAsync(_scenario.OwnerUsername, KeyType.AsymmetricSignature);
        var payload = Encoding.UTF8.GetBytes("signed as the account");
        using var signer = ECDsa.Create();
        signer.ImportPkcs8PrivateKey(Convert.FromBase64String(revealed.ReadKey()!), out _);
        var signature = signer.SignData(payload, HashAlgorithmName.SHA256);

        var keySet = await GetKeySetAsync();

        var jwk = keySet.Keys.Single(k => k.Use == AccountJsonWebKey.USE_SIGNATURE);
        using var verifier = ECDsa.Create(
            new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = Base64UrlEncoder.DecodeBytes(jwk.X),
                    Y = Base64UrlEncoder.DecodeBytes(jwk.Y),
                },
            }
        );
        Assert.True(verifier.VerifyData(payload, signature, HashAlgorithmName.SHA256));
    }

    [Fact]
    public async Task AccountSignature_VerifiesFromTheKeySet_ForAStandardClient()
    {
        var (payload, signature) = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            async services =>
            {
                var provider = (
                    await services
                        .GetRequiredService<IRetrievalService>()
                        .GetAccountAsymmetricSignatureProviderAsync(_scenario.AccountId)
                ).Item!;
                return ("a payload", provider.Sign("a payload"));
            }
        );

        var keySet = await GetKeySetAsync();

        var jwk = keySet.Keys.Single(k => k.Use == AccountJsonWebKey.USE_SIGNATURE);
        using var verifier = ECDsa.Create(
            new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = Base64UrlEncoder.DecodeBytes(jwk.X),
                    Y = Base64UrlEncoder.DecodeBytes(jwk.Y),
                },
            }
        );
        Assert.True(
            verifier.VerifyData(
                Encoding.UTF8.GetBytes(payload),
                Convert.FromBase64String(signature),
                HashAlgorithmName.SHA256
            )
        );
    }

    [Fact]
    public async Task SystemContext_ReachesARealAccountsKeys_AndDecryptsWhatAMemberEncrypted()
    {
        var ciphertext = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            async services =>
                (
                    await services
                        .GetRequiredService<IRetrievalService>()
                        .GetAccountSymmetricEncryptionProviderAsync(_scenario.AccountId)
                ).Item!.Encrypt("workflow value")
        );

        var (plaintext, asymmetric, signature) = await _scenario.AsSystemAsync(async services =>
        {
            var retrieval = services.GetRequiredService<IRetrievalService>();
            var symmetric = await retrieval.GetAccountSymmetricEncryptionProviderAsync(
                _scenario.AccountId
            );
            var encryption = await retrieval.GetAccountAsymmetricEncryptionProviderAsync(
                _scenario.AccountId
            );
            var signing = await retrieval.GetAccountAsymmetricSignatureProviderAsync(
                _scenario.AccountId
            );
            return (symmetric.Item!.Decrypt(ciphertext), encryption.ResultCode, signing.ResultCode);
        });

        Assert.Equal("workflow value", plaintext);
        Assert.Equal(RetrieveResultCode.Success, asymmetric);
        Assert.Equal(RetrieveResultCode.Success, signature);
    }

    [Fact]
    public async Task SystemContext_GetsNotFound_ForAnAccountThatDoesNotExist()
    {
        var result = await _scenario.AsSystemAsync(services =>
            services
                .GetRequiredService<IRetrievalService>()
                .GetAccountSymmetricEncryptionProviderAsync(Guid.CreateVersion7())
        );

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Item);
    }

    [Fact]
    public async Task KeySet_KeepsTheOldSigningKey_AfterARotation()
    {
        var before = await GetKeySetAsync();
        var oldKey = before.Keys.Single(k => k.Use == AccountJsonWebKey.USE_SIGNATURE);

        await RotateAsync(KeyType.AsymmetricSignature);
        await RotateAsync(KeyType.AsymmetricEncryption);

        var after = await GetKeySetAsync();
        var signing = after.Keys.Where(k => k.Use == AccountJsonWebKey.USE_SIGNATURE).ToList();
        Assert.Equal(2, signing.Count);
        Assert.Equal(oldKey, signing[1]);
        Assert.EndsWith("-sig-2", signing[0].KeyId);
        Assert.EndsWith(
            "-enc-2",
            after.Keys.Single(k => k.Use == AccountJsonWebKey.USE_ENCRYPTION).KeyId
        );
    }

    [Fact]
    public async Task Reveal_ReturnsTheNewestVersion_AfterARotation()
    {
        await RotateAsync(KeyType.SymmetricEncryption);

        var revealed = await RevealAsync(_scenario.OwnerUsername, KeyType.SymmetricEncryption);

        Assert.Equal(2, revealed.Version);
    }

    [Fact]
    public async Task Reveal_Refuses_AMemberWithoutThePermission()
    {
        var result = await RevealAsync(_scenario.DirectMemberUsername, KeyType.SymmetricEncryption);

        Assert.Equal(RevealAccountKeyResultCode.UnauthorizedError, result.ResultCode);
        Assert.Null(result.ReadKey());
    }

    [Fact]
    public async Task Reveal_Refuses_AWrongPassword()
    {
        var result = await RevealAsync(
            _scenario.OwnerUsername,
            KeyType.AsymmetricSignature,
            "not-the-password"
        );

        Assert.Equal(RevealAccountKeyResultCode.InvalidCurrentPasswordError, result.ResultCode);
        Assert.Null(result.ReadKey());
    }

    [Fact]
    public async Task Reveal_NeedsTheSecondFactor_ForAUserWhoHasIt()
    {
        await _scenario.EnrollTwoFactorAsync(_scenario.OwnerUsername);

        var result = await RevealAsync(_scenario.OwnerUsername, KeyType.SymmetricEncryption);

        Assert.Equal(RevealAccountKeyResultCode.TotpRequiredError, result.ResultCode);
        Assert.Null(result.ReadKey());
    }

    [Fact]
    public async Task Reveal_IsAlwaysAudited_EvenThoughTheAccountRecordsNoExecute()
    {
        await RevealAsync(_scenario.OwnerUsername, KeyType.SymmetricEncryption);
        await RevealAsync(_scenario.DirectMemberUsername, KeyType.SymmetricEncryption);

        var codes = await _scenario.AsSystemAsync(services =>
            services
                .GetRequiredService<IamDbContext>()
                .Set<AuditEntryEntity>()
                .Where(e =>
                    e.Operation == nameof(IRetrievalService.RevealAccountKeyAsync)
                    && e.ResourceType == Account.SECRET_KEYS_RESOURCE_TYPE
                    && e.AccountId == _scenario.AccountId
                )
                .Select(e => e.ResultCode)
                .ToListAsync()
        );

        Assert.Contains(nameof(RevealAccountKeyResultCode.Success), codes);
        Assert.Contains(nameof(RevealAccountKeyResultCode.UnauthorizedError), codes);
    }
}
