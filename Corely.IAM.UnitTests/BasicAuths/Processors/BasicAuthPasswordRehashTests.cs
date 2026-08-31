using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.GoogleAuths.Entities;
using Corely.IAM.Validators;
using Corely.Security.Hashing;
using Corely.Security.Hashing.Factories;
using Corely.Security.Hashing.Providers;
using Corely.Security.PasswordValidation.Providers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.BasicAuths.Processors;

/// <summary>
/// Changing the default hash only affects passwords set afterwards. Without an upgrade on sign-in,
/// every existing account keeps its original hash forever - so this behaviour, not the default
/// change, is what actually migrates stored passwords off the weaker algorithm.
/// </summary>
public class BasicAuthPasswordRehashTests
{
    private const string VALID_PASSWORD = "Password1!";

    private readonly ServiceFactory _serviceFactory = new();
    private readonly IRepo<BasicAuthEntity> _basicAuthRepo;
    private readonly IHashProviderFactory _hashProviderFactory;
    private readonly BasicAuthProcessor _processor;

    public BasicAuthPasswordRehashTests()
    {
        _basicAuthRepo = _serviceFactory.GetRequiredService<IRepo<BasicAuthEntity>>();
        _hashProviderFactory = _serviceFactory.GetRequiredService<IHashProviderFactory>();

        _processor = new BasicAuthProcessor(
            _basicAuthRepo,
            _serviceFactory.GetRequiredService<IReadonlyRepo<GoogleAuthEntity>>(),
            _serviceFactory.GetRequiredService<IPasswordValidationProvider>(),
            _hashProviderFactory,
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<ILogger<BasicAuthProcessor>>()
        );
    }

    [Fact]
    public async Task VerifyingALegacyHash_UpgradesItToTheCurrentProvider()
    {
        var userId = await CreateLegacyBasicAuthAsync();

        var result = await _processor.VerifyBasicAuthAsync(
            new VerifyBasicAuthRequest(userId, VALID_PASSWORD)
        );

        Assert.True(result.IsValid);
        var stored = await GetStoredHashAsync(userId);
        Assert.StartsWith(HashConstants.PBKDF2_SHA256_CODE, stored);
    }

    [Fact]
    public async Task TheUpgradedHash_StillVerifiesTheSamePassword()
    {
        var userId = await CreateLegacyBasicAuthAsync();

        await _processor.VerifyBasicAuthAsync(new VerifyBasicAuthRequest(userId, VALID_PASSWORD));
        var second = await _processor.VerifyBasicAuthAsync(
            new VerifyBasicAuthRequest(userId, VALID_PASSWORD)
        );

        Assert.True(second.IsValid);
    }

    [Fact]
    public async Task AFailedVerification_DoesNotUpgradeTheHash()
    {
        var userId = await CreateLegacyBasicAuthAsync();

        var result = await _processor.VerifyBasicAuthAsync(
            new VerifyBasicAuthRequest(userId, "WrongPassword1!")
        );

        Assert.False(result.IsValid);
        var stored = await GetStoredHashAsync(userId);
        Assert.StartsWith(HashConstants.SALTED_SHA256_CODE, stored);
    }

    [Fact]
    public async Task AHashAlreadyOnTheCurrentProvider_IsNotRewritten()
    {
        var userId = Guid.CreateVersion7();
        await _processor.CreateBasicAuthAsync(new CreateBasicAuthRequest(userId, VALID_PASSWORD));
        var before = await GetStoredHashAsync(userId);

        var result = await _processor.VerifyBasicAuthAsync(
            new VerifyBasicAuthRequest(userId, VALID_PASSWORD)
        );

        Assert.True(result.IsValid);
        Assert.Equal(before, await GetStoredHashAsync(userId));
    }

    /// <summary>
    /// Writes a hash in the format used before PBKDF2 became the default, standing in for a row
    /// created by an earlier version of the library.
    /// </summary>
    private async Task<Guid> CreateLegacyBasicAuthAsync()
    {
        var userId = Guid.CreateVersion7();
        var legacyHash = new Sha256SaltedHashProvider().Hash(VALID_PASSWORD);

        await _basicAuthRepo.CreateAsync(
            new BasicAuthEntity
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                Password = legacyHash,
            }
        );

        return userId;
    }

    private async Task<string> GetStoredHashAsync(Guid userId)
    {
        var entity = await _basicAuthRepo.GetAsync(e => e.UserId == userId);
        Assert.NotNull(entity);
        return entity!.Password;
    }
}
