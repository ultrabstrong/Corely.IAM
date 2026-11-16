using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.ApiKeys.Entities;
using Corely.IAM.Audits.Entities;
using Corely.IAM.EmailVerifications.Entities;
using Corely.IAM.Invitations.Entities;
using Corely.IAM.MfaChallenges.Entities;
using Corely.IAM.Security.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Accounts.Processors;

internal class AccountClosureProcessor(
    IRepo<AccountEntity> accountRepo,
    IRepo<ApiKeyEntity> apiKeyRepo,
    IRepo<InvitationEntity> invitationRepo,
    IRepo<AuditEntryEntity> auditEntryRepo,
    IRepo<AuthTokenEntity> authTokenRepo,
    IRepo<MfaChallengeEntity> mfaChallengeRepo,
    IRepo<EmailVerificationChallengeEntity> emailVerificationChallengeRepo,
    AccountDeletionPolicy policy,
    TimeProvider timeProvider,
    ILogger<AccountClosureProcessor> logger
) : IAccountClosureProcessor
{
    private readonly IRepo<AccountEntity> _accountRepo = accountRepo.ThrowIfNull(
        nameof(accountRepo)
    );
    private readonly IRepo<ApiKeyEntity> _apiKeyRepo = apiKeyRepo.ThrowIfNull(nameof(apiKeyRepo));
    private readonly IRepo<InvitationEntity> _invitationRepo = invitationRepo.ThrowIfNull(
        nameof(invitationRepo)
    );
    private readonly IRepo<AuditEntryEntity> _auditEntryRepo = auditEntryRepo.ThrowIfNull(
        nameof(auditEntryRepo)
    );
    private readonly IRepo<AuthTokenEntity> _authTokenRepo = authTokenRepo.ThrowIfNull(
        nameof(authTokenRepo)
    );
    private readonly IRepo<MfaChallengeEntity> _mfaChallengeRepo = mfaChallengeRepo.ThrowIfNull(
        nameof(mfaChallengeRepo)
    );
    private readonly IRepo<EmailVerificationChallengeEntity> _emailVerificationChallengeRepo =
        emailVerificationChallengeRepo.ThrowIfNull(nameof(emailVerificationChallengeRepo));
    private readonly AccountDeletionPolicy _policy = policy.ThrowIfNull(nameof(policy));
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));
    private readonly ILogger<AccountClosureProcessor> _logger = logger.ThrowIfNull(nameof(logger));

    public async Task<CloseAccountResult> RemoveAccountAccessAsync(Guid accountId)
    {
        var account = await _accountRepo.GetAsync(
            a => a.Id == accountId,
            include: q => q.Include(a => a.Users)
        );
        if (Refusal(account, accountId) is { } refusal)
            return refusal;

        var members = account!.Users?.Count ?? 0;
        account.Users?.Clear();
        await _accountRepo.UpdateAsync(account);

        var apiKeys = await _apiKeyRepo.ListAsync(
            k => k.AccountId == accountId,
            include: q => q.Include(k => k.Roles).Include(k => k.Groups)
        );
        foreach (var apiKey in apiKeys)
        {
            apiKey.Roles?.Clear();
            apiKey.Groups?.Clear();
            await _apiKeyRepo.UpdateAsync(apiKey);
            await _apiKeyRepo.DeleteAsync(apiKey);
        }

        var pending = await _invitationRepo.ListAsync(i =>
            i.AccountId == accountId && i.AcceptedByUserId == null && i.RevokedUtc == null
        );
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        foreach (var invitation in pending)
        {
            invitation.RevokedUtc = utcNow;
            await _invitationRepo.UpdateAsync(invitation);
        }

        _logger.LogInformation(
            "Removed access to account {AccountId}: {MemberCount} members, {ApiKeyCount} API keys and {InvitationCount} pending invitations",
            accountId,
            members,
            apiKeys.Count,
            pending.Count
        );
        return new CloseAccountResult(CloseAccountResultCode.Success, string.Empty);
    }

    public async Task<CloseAccountResult> PurgeAccountDataAsync(Guid accountId)
    {
        var account = await _accountRepo.GetAsync(a => a.Id == accountId);
        if (Refusal(account, accountId) is { } refusal)
            return refusal;

        if (!_policy.PurgesAccountData)
            return new CloseAccountResult(CloseAccountResultCode.Success, string.Empty);

        var auditEntries = await _auditEntryRepo.ExecuteDeleteAsync(e => e.AccountId == accountId);
        var authTokens = await _authTokenRepo.ExecuteDeleteAsync(t => t.AccountId == accountId);
        var mfaChallenges = await _mfaChallengeRepo.ExecuteDeleteAsync(c =>
            c.AccountId == accountId
        );
        var emailChallenges = await _emailVerificationChallengeRepo.ExecuteDeleteAsync(c =>
            c.AccountId == accountId
        );

        _logger.LogInformation(
            "Purged account {AccountId} before deleting it: {AuditEntryCount} audit entries, {AuthTokenCount} auth tokens, {MfaChallengeCount} MFA challenges and {EmailVerificationChallengeCount} email verification challenges",
            accountId,
            auditEntries,
            authTokens,
            mfaChallenges,
            emailChallenges
        );
        return new CloseAccountResult(CloseAccountResultCode.Success, string.Empty);
    }

    private CloseAccountResult? Refusal(AccountEntity? account, Guid accountId)
    {
        if (account == null)
        {
            _logger.LogWarning("Account with Id {AccountId} not found", accountId);
            return new CloseAccountResult(
                CloseAccountResultCode.AccountNotFoundError,
                $"Account with Id {accountId} not found"
            );
        }

        if (account.IsPlatformAccount)
        {
            _logger.LogWarning("Refused closing the platform account {AccountId}", accountId);
            return new CloseAccountResult(
                CloseAccountResultCode.PlatformAccountError,
                "The platform account cannot be deleted"
            );
        }

        return null;
    }
}
