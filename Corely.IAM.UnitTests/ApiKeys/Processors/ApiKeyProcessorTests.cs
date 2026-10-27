using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.ApiKeys.Entities;
using Corely.IAM.ApiKeys.Enums;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.ApiKeys.Processors;
using Corely.IAM.ApiKeys.Providers;
using Corely.IAM.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Security.Models;
using Corely.IAM.Validators;
using Corely.Security.Hashing.Factories;
using Corely.Security.Secrets;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.ApiKeys.Processors;

public class ApiKeyProcessorTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly Mock<IApiKeyAuthenticationProvider> _mockApiKeyAuthenticationProvider = new();
    private readonly DateTime _utcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private SecurityPolicy _policy = SecurityPolicy.Default with { ApiKeysEnabled = true };

    private ApiKeyProcessor CreateProcessor() =>
        new(
            _serviceFactory.GetRequiredService<IRepo<ApiKeyEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<ApiKeySecretEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<RoleEntity>>(),
            _mockApiKeyAuthenticationProvider.Object,
            new FixedSecurityPolicyProvider(_policy),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<IHashProviderFactory>(),
            _serviceFactory.GetRequiredService<IamHashCodes>(),
            _serviceFactory.GetRequiredService<ISecretProvider>(),
            new FixedTimeProvider(_utcNow),
            _serviceFactory.GetRequiredService<ILogger<ApiKeyProcessor>>()
        );

    private async Task<AccountEntity> CreateAccountAsync(bool isPlatformAccount = false) =>
        await _serviceFactory
            .GetRequiredService<IRepo<AccountEntity>>()
            .CreateAsync(
                new AccountEntity
                {
                    Id = Guid.CreateVersion7(),
                    AccountName = "Acme",
                    IsPlatformAccount = isPlatformAccount,
                }
            );

    private async Task<RoleEntity> CreateRoleAsync(Guid accountId) =>
        await _serviceFactory
            .GetRequiredService<IRepo<RoleEntity>>()
            .CreateAsync(
                new RoleEntity
                {
                    Id = Guid.CreateVersion7(),
                    Name = $"role-{Guid.CreateVersion7()}",
                    AccountId = accountId,
                }
            );

    private async Task<CreateApiKeyResult> CreateKeyAsync(Guid accountId, string name = "ci") =>
        await CreateProcessor().CreateApiKeyAsync(new CreateApiKeyRequest(name, accountId));

    [Fact]
    public async Task CreateApiKey_ReturnsKeyOnce_AndStoresOnlyItsHash()
    {
        var account = await CreateAccountAsync();

        var result = await CreateKeyAsync(account.Id);

        Assert.Equal(CreateApiKeyResultCode.Success, result.ResultCode);
        Assert.True(ApiKeyCredential.TryParse(result.Key, out var credential));
        var secret = Assert.Single(
            await _serviceFactory
                .GetRequiredService<IRepo<ApiKeySecretEntity>>()
                .ListAsync(s => s.ApiKeyId == result.CreatedId)
        );
        Assert.Equal(ApiKeySlot.Primary, secret.Slot);
        Assert.Equal(credential.Prefix, secret.Prefix);
        Assert.DoesNotContain(credential.Secret, secret.SecretHash);
        Assert.Equal(_utcNow.AddDays(_policy.ApiKeyMaxLifetimeDays), secret.ExpiresUtc);
    }

    [Fact]
    public async Task CreateApiKey_ReturnsDisabled_WhenApiKeysAreOff()
    {
        _policy = SecurityPolicy.Default;
        var account = await CreateAccountAsync();

        var result = await CreateKeyAsync(account.Id);

        Assert.Equal(CreateApiKeyResultCode.ApiKeysDisabledError, result.ResultCode);
        Assert.Null(result.Key);
    }

    [Fact]
    public async Task CreateApiKey_ReturnsDisabled_InThePlatformAccount_UnlessItsSwitchIsOn()
    {
        var platform = await CreateAccountAsync(isPlatformAccount: true);

        var refused = await CreateKeyAsync(platform.Id);
        _policy = _policy with { PlatformAccountApiKeysEnabled = true };
        var allowed = await CreateKeyAsync(platform.Id, "allowed");

        Assert.Equal(CreateApiKeyResultCode.ApiKeysDisabledError, refused.ResultCode);
        Assert.Equal(CreateApiKeyResultCode.Success, allowed.ResultCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateApiKey_ReturnsValidationError_ForABlankName(string name)
    {
        var account = await CreateAccountAsync();

        var result = await CreateKeyAsync(account.Id, name);

        Assert.Equal(CreateApiKeyResultCode.ValidationError, result.ResultCode);
    }

    [Fact]
    public async Task CreateApiKey_ReturnsValidationError_ForATooLongName()
    {
        var account = await CreateAccountAsync();

        var result = await CreateKeyAsync(account.Id, new string('a', ApiKey.NAME_MAX_LENGTH + 1));

        Assert.Equal(CreateApiKeyResultCode.ValidationError, result.ResultCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(SecurityPolicy.API_KEY_MAX_LIFETIME_DAYS_DEFAULT + 1)]
    public async Task CreateApiKey_ReturnsValidationError_ForAnExpiryOutsideThePolicy(int days)
    {
        var account = await CreateAccountAsync();

        var result = await CreateProcessor()
            .CreateApiKeyAsync(
                new CreateApiKeyRequest("ci", account.Id, ExpiresUtc: _utcNow.AddDays(days))
            );

        Assert.Equal(CreateApiKeyResultCode.ValidationError, result.ResultCode);
    }

    [Fact]
    public async Task CreateApiKey_ReturnsAccountNotFound_ForAnUnknownAccount()
    {
        var result = await CreateKeyAsync(Guid.CreateVersion7());

        Assert.Equal(CreateApiKeyResultCode.AccountNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task CreateApiKey_ReturnsApiKeyExists_ForATrimmedDuplicateName()
    {
        var account = await CreateAccountAsync();
        await CreateKeyAsync(account.Id, "ci");

        var result = await CreateKeyAsync(account.Id, "  ci  ");

        Assert.Equal(CreateApiKeyResultCode.ApiKeyExistsError, result.ResultCode);
    }

    [Fact]
    public async Task RotateApiKeySecret_ReplacesTheSlot_AndEndsItsSessions()
    {
        var account = await CreateAccountAsync();
        var created = await CreateKeyAsync(account.Id);

        var rotated = await CreateProcessor()
            .RotateApiKeySecretAsync(
                new RotateApiKeySecretRequest(created.CreatedId, account.Id, ApiKeySlot.Primary)
            );

        Assert.Equal(RotateApiKeySecretResultCode.Success, rotated.ResultCode);
        Assert.NotEqual(created.Key, rotated.Key);
        _mockApiKeyAuthenticationProvider.Verify(
            x => x.RevokeApiKeySlotAuthTokensAsync(created.CreatedId, ApiKeySlot.Primary),
            Times.Once
        );
    }

    [Fact]
    public async Task RotateApiKeySecret_IssuesTheOtherSlot_WithoutTouchingTheFirst()
    {
        var account = await CreateAccountAsync();
        var created = await CreateKeyAsync(account.Id);

        await CreateProcessor()
            .RotateApiKeySecretAsync(
                new RotateApiKeySecretRequest(created.CreatedId, account.Id, ApiKeySlot.Secondary)
            );

        var slots = (
            await _serviceFactory
                .GetRequiredService<IRepo<ApiKeySecretEntity>>()
                .ListAsync(s => s.ApiKeyId == created.CreatedId)
        )
            .Select(s => s.Slot)
            .Order();
        Assert.Equal([ApiKeySlot.Primary, ApiKeySlot.Secondary], slots);
    }

    [Fact]
    public async Task RotateApiKeySecret_ReturnsNotFound_ForAKeyInAnotherAccount()
    {
        var account = await CreateAccountAsync();
        var other = await CreateAccountAsync();
        var created = await CreateKeyAsync(account.Id);

        var rotated = await CreateProcessor()
            .RotateApiKeySecretAsync(
                new RotateApiKeySecretRequest(created.CreatedId, other.Id, ApiKeySlot.Primary)
            );

        Assert.Equal(RotateApiKeySecretResultCode.ApiKeyNotFoundError, rotated.ResultCode);
        _mockApiKeyAuthenticationProvider.Verify(
            x => x.RevokeApiKeySlotAuthTokensAsync(It.IsAny<Guid>(), It.IsAny<ApiKeySlot>()),
            Times.Never
        );
    }

    [Fact]
    public async Task UpdateApiKey_TrimsTheName_AndRefusesADuplicate()
    {
        var account = await CreateAccountAsync();
        var first = await CreateKeyAsync(account.Id, "first");
        var second = await CreateKeyAsync(account.Id, "second");
        var processor = CreateProcessor();

        var renamed = await processor.UpdateApiKeyAsync(
            new UpdateApiKeyRequest(first.CreatedId, account.Id, "  renamed  ", "  why  ")
        );
        var duplicate = await processor.UpdateApiKeyAsync(
            new UpdateApiKeyRequest(second.CreatedId, account.Id, "renamed", null)
        );

        Assert.Equal(ModifyResultCode.Success, renamed.ResultCode);
        Assert.Equal(ModifyResultCode.ValidationError, duplicate.ResultCode);
        var stored = await _serviceFactory
            .GetRequiredService<IRepo<ApiKeyEntity>>()
            .GetAsync(k => k.Id == first.CreatedId);
        Assert.Equal("renamed", stored!.Name);
        Assert.Equal("why", stored.Description);
    }

    [Fact]
    public async Task UpdateApiKey_ReturnsValidationError_ForABlankName()
    {
        var account = await CreateAccountAsync();
        var created = await CreateKeyAsync(account.Id);

        var result = await CreateProcessor()
            .UpdateApiKeyAsync(new UpdateApiKeyRequest(created.CreatedId, account.Id, " ", null));

        Assert.Equal(ModifyResultCode.ValidationError, result.ResultCode);
    }

    [Fact]
    public async Task DeleteApiKey_RemovesTheKey_AndReturnsNotFoundAfter()
    {
        var account = await CreateAccountAsync();
        var created = await CreateKeyAsync(account.Id);
        var processor = CreateProcessor();

        var deleted = await processor.DeleteApiKeyAsync(created.CreatedId, account.Id);
        var again = await processor.DeleteApiKeyAsync(created.CreatedId, account.Id);

        Assert.Equal(DeleteApiKeyResultCode.Success, deleted.ResultCode);
        Assert.Equal(DeleteApiKeyResultCode.ApiKeyNotFoundError, again.ResultCode);
    }

    [Fact]
    public async Task AssignRolesToApiKey_SkipsHeldUnknownAndForeignRoles()
    {
        var account = await CreateAccountAsync();
        var other = await CreateAccountAsync();
        var held = await CreateRoleAsync(account.Id);
        var fresh = await CreateRoleAsync(account.Id);
        var foreign = await CreateRoleAsync(other.Id);
        var created = await CreateKeyAsync(account.Id);
        var processor = CreateProcessor();
        await processor.AssignRolesToApiKeyAsync(
            new AssignRolesToApiKeyRequest([held.Id], created.CreatedId, account.Id)
        );

        var result = await processor.AssignRolesToApiKeyAsync(
            new AssignRolesToApiKeyRequest(
                [held.Id, fresh.Id, foreign.Id, Guid.CreateVersion7()],
                created.CreatedId,
                account.Id
            )
        );

        Assert.Equal(AssignRolesToApiKeyResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(1, result.AddedRoleCount);
        Assert.Equal(3, result.InvalidRoleIds.Count);
        Assert.DoesNotContain(fresh.Id, result.InvalidRoleIds);
    }

    [Fact]
    public async Task AssignRolesToApiKey_ReturnsInvalidRoleIds_WhenNoneApply()
    {
        var account = await CreateAccountAsync();
        var created = await CreateKeyAsync(account.Id);

        var result = await CreateProcessor()
            .AssignRolesToApiKeyAsync(
                new AssignRolesToApiKeyRequest(
                    [Guid.CreateVersion7()],
                    created.CreatedId,
                    account.Id
                )
            );

        Assert.Equal(AssignRolesToApiKeyResultCode.InvalidRoleIdsError, result.ResultCode);
    }

    [Fact]
    public async Task AssignRolesToApiKey_ReturnsNotFound_ForAnUnknownKey()
    {
        var account = await CreateAccountAsync();
        var role = await CreateRoleAsync(account.Id);

        var result = await CreateProcessor()
            .AssignRolesToApiKeyAsync(
                new AssignRolesToApiKeyRequest([role.Id], Guid.CreateVersion7(), account.Id)
            );

        Assert.Equal(AssignRolesToApiKeyResultCode.ApiKeyNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task RemoveRolesFromApiKey_RemovesHeldRoles_AndReportsTheRest()
    {
        var account = await CreateAccountAsync();
        var held = await CreateRoleAsync(account.Id);
        var created = await CreateKeyAsync(account.Id);
        var processor = CreateProcessor();
        await processor.AssignRolesToApiKeyAsync(
            new AssignRolesToApiKeyRequest([held.Id], created.CreatedId, account.Id)
        );
        var notHeld = Guid.CreateVersion7();

        var result = await processor.RemoveRolesFromApiKeyAsync(
            new RemoveRolesFromApiKeyRequest([held.Id, notHeld], created.CreatedId, account.Id)
        );
        var again = await processor.RemoveRolesFromApiKeyAsync(
            new RemoveRolesFromApiKeyRequest([held.Id], created.CreatedId, account.Id)
        );

        Assert.Equal(RemoveRolesFromApiKeyResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(1, result.RemovedRoleCount);
        Assert.Equal([notHeld], result.InvalidRoleIds);
        Assert.Equal(RemoveRolesFromApiKeyResultCode.InvalidRoleIdsError, again.ResultCode);
    }

    [Fact]
    public async Task GetApiKeyById_HydratesRoles_AndIsScopedToTheAccount()
    {
        var account = await CreateAccountAsync();
        var role = await CreateRoleAsync(account.Id);
        var created = await CreateKeyAsync(account.Id);
        var processor = CreateProcessor();
        await processor.AssignRolesToApiKeyAsync(
            new AssignRolesToApiKeyRequest([role.Id], created.CreatedId, account.Id)
        );

        var hydrated = await processor.GetApiKeyByIdAsync(created.CreatedId, true, account.Id);
        var other = await processor.GetApiKeyByIdAsync(
            created.CreatedId,
            false,
            Guid.CreateVersion7()
        );

        Assert.Equal(RetrieveResultCode.Success, hydrated.ResultCode);
        Assert.Equal(role.Id, Assert.Single(hydrated.Data!.Roles!).Id);
        Assert.Equal(RetrieveResultCode.NotFoundError, other.ResultCode);
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
