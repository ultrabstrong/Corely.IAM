using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Persistence;

/// <summary>
/// I7 - key provisioning, storage, and round-trip against a real database.
///
/// The rule this tier can actually enforce is that nothing sensitive is stored in the clear: keys
/// are encrypted with the system key before persisting, so the stored column must never equal the
/// value the provider hands back.
/// </summary>
public class KeyManagementTests(IamScenario scenario) : IClassFixture<IamScenario>
{
    [Fact]
    public async Task RegisteringAUser_ProvisionsBothKeyTypes()
    {
        var symmetric = await scenario.Host.QueryAsync(db =>
            db.Users.AsNoTracking()
                .Where(u => u.Id == scenario.OwnerUserId)
                .SelectMany(u => u.SymmetricKeys!)
                .CountAsync()
        );
        var asymmetric = await scenario.Host.QueryAsync(db =>
            db.Users.AsNoTracking()
                .Where(u => u.Id == scenario.OwnerUserId)
                .SelectMany(u => u.AsymmetricKeys!)
                .CountAsync()
        );

        Assert.True(symmetric > 0, "A registered user must have a symmetric key.");
        Assert.True(asymmetric > 0, "A registered user must have an asymmetric key.");
    }

    [Fact]
    public async Task SymmetricEncryption_RoundTrips()
    {
        const string plaintext = "sensitive-value";

        var result = await scenario.ActAsAsync(
            scenario.OwnerUsername,
            scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IRetrievalService>()
                    .GetUserSymmetricEncryptionProviderAsync()
        );
        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);

        var ciphertext = result.Item!.Encrypt(plaintext);

        Assert.NotEqual(plaintext, ciphertext);
        Assert.Equal(plaintext, result.Item.Decrypt(ciphertext));
    }

    [Fact]
    public async Task AsymmetricSignature_RoundTrips()
    {
        const string payload = "payload-to-sign";

        var result = await scenario.ActAsAsync(
            scenario.OwnerUsername,
            scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IRetrievalService>()
                    .GetUserAsymmetricSignatureProviderAsync()
        );
        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);

        var signature = result.Item!.Sign(payload);

        Assert.True(result.Item.Verify(payload, signature));
        Assert.False(result.Item.Verify("tampered-payload", signature));
    }

    [Fact]
    public async Task StoredPrivateKeys_AreNotPlaintext()
    {
        var provider = await scenario.ActAsAsync(
            scenario.OwnerUsername,
            scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IRetrievalService>()
                    .GetUserAsymmetricSignatureProviderAsync()
        );
        Assert.Equal(RetrieveResultCode.Success, provider.ResultCode);

        var storedPrivateKeys = await scenario.Host.QueryAsync(db =>
            db.Users.AsNoTracking()
                .Where(u => u.Id == scenario.OwnerUserId)
                .SelectMany(u => u.AsymmetricKeys!)
                .Select(k => k.EncryptedPrivateKey)
                .ToListAsync()
        );

        Assert.NotEmpty(storedPrivateKeys);
        Assert.All(
            storedPrivateKeys,
            stored =>
            {
                Assert.False(string.IsNullOrWhiteSpace(stored));
                Assert.NotEqual(provider.Item!.PublicKey, stored);
                Assert.DoesNotContain("PRIVATE KEY", stored);
            }
        );
    }

    [Fact]
    public async Task EachUserGetsDistinctKeys()
    {
        var ownerKeys = await PublicKeysForAsync(scenario.OwnerUserId);
        var memberKeys = await PublicKeysForAsync(scenario.DirectMemberUserId);

        Assert.NotEmpty(ownerKeys);
        Assert.NotEmpty(memberKeys);
        Assert.Empty(ownerKeys.Intersect(memberKeys));
    }

    [Fact]
    public async Task AccountKeysAreProvisionedToo()
    {
        var result = await scenario.ActAsAsync(
            scenario.OwnerUsername,
            scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IRetrievalService>()
                    .GetAccountSymmetricEncryptionProviderAsync(scenario.AccountId)
        );

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.Equal("round-trip", result.Item!.Decrypt(result.Item.Encrypt("round-trip")));
    }

    private Task<List<string>> PublicKeysForAsync(Guid userId) =>
        scenario.Host.QueryAsync(db =>
            db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .SelectMany(u => u.AsymmetricKeys!)
                .Select(k => k.PublicKey)
                .ToListAsync()
        );
}
