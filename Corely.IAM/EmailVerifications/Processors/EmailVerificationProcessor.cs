using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.EmailVerifications.Entities;
using Corely.IAM.EmailVerifications.Models;
using Corely.IAM.EmailVerifications.Providers;
using Corely.IAM.FederatedIdentities.Entities;
using Corely.IAM.PasswordRecoveries.Entities;
using Corely.IAM.Platform.Providers;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.TotpAuths.Entities;
using Corely.IAM.Users.Entities;
using Corely.Security.Hashing.Factories;
using Corely.Security.Secrets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.EmailVerifications.Processors;

internal class EmailVerificationProcessor(
    IRepo<EmailVerificationEntity> verificationRepo,
    IRepo<EmailVerificationChallengeEntity> challengeRepo,
    IRepo<UserEntity> userRepo,
    IRepo<BasicAuthEntity> basicAuthRepo,
    IRepo<TotpAuthEntity> totpAuthRepo,
    IRepo<FederatedIdentityEntity> federatedIdentityRepo,
    IRepo<PasswordRecoveryEntity> passwordRecoveryRepo,
    IAuthenticationProvider authenticationProvider,
    ISecurityPolicyProvider securityPolicyProvider,
    EmailVerificationRequirement requirement,
    IHashProviderFactory hashProviderFactory,
    IamHashCodes hashCodes,
    ISecretProvider secretProvider,
    TimeProvider timeProvider,
    ILogger<EmailVerificationProcessor> logger
) : IEmailVerificationProcessor
{
    private readonly IRepo<EmailVerificationEntity> _verificationRepo =
        verificationRepo.ThrowIfNull(nameof(verificationRepo));
    private readonly IRepo<EmailVerificationChallengeEntity> _challengeRepo =
        challengeRepo.ThrowIfNull(nameof(challengeRepo));
    private readonly IRepo<UserEntity> _userRepo = userRepo.ThrowIfNull(nameof(userRepo));
    private readonly IRepo<BasicAuthEntity> _basicAuthRepo = basicAuthRepo.ThrowIfNull(
        nameof(basicAuthRepo)
    );
    private readonly IRepo<TotpAuthEntity> _totpAuthRepo = totpAuthRepo.ThrowIfNull(
        nameof(totpAuthRepo)
    );
    private readonly IRepo<FederatedIdentityEntity> _federatedIdentityRepo =
        federatedIdentityRepo.ThrowIfNull(nameof(federatedIdentityRepo));
    private readonly IRepo<PasswordRecoveryEntity> _passwordRecoveryRepo =
        passwordRecoveryRepo.ThrowIfNull(nameof(passwordRecoveryRepo));
    private readonly IAuthenticationProvider _authenticationProvider =
        authenticationProvider.ThrowIfNull(nameof(authenticationProvider));
    private readonly ISecurityPolicyProvider _securityPolicyProvider =
        securityPolicyProvider.ThrowIfNull(nameof(securityPolicyProvider));
    private readonly EmailVerificationRequirement _requirement = requirement.ThrowIfNull(
        nameof(requirement)
    );
    private readonly IHashProviderFactory _hashProviderFactory = hashProviderFactory.ThrowIfNull(
        nameof(hashProviderFactory)
    );
    private readonly IamHashCodes _hashCodes = hashCodes.ThrowIfNull(nameof(hashCodes));
    private readonly ISecretProvider _secretProvider = secretProvider.ThrowIfNull(
        nameof(secretProvider)
    );
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));
    private readonly ILogger<EmailVerificationProcessor> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<RequestEmailVerificationResult> RequestForUserAsync(Guid userId)
    {
        var user = await _userRepo.GetAsync(u => u.Id == userId);
        if (user == null)
            return new(RequestEmailVerificationResultCode.UserNotFoundError, "User not found");

        return await SendLinkAsync(user);
    }

    public async Task<RequestEmailVerificationResult> RequestForChallengeAsync(
        string challengeToken
    )
    {
        var challenge = await FindLiveChallengeAsync(challengeToken);
        if (challenge == null)
            return new(
                RequestEmailVerificationResultCode.ChallengeNotFoundError,
                "Sign in again to get a new link"
            );

        var user = await _userRepo.GetAsync(u => u.Id == challenge.UserId);
        if (user == null)
            return new(RequestEmailVerificationResultCode.UserNotFoundError, "User not found");

        var result = await SendLinkAsync(user);
        if (result.ResultCode == RequestEmailVerificationResultCode.Success)
            await ExtendChallengeAsync(challenge, result.ExpiresUtc!.Value);

        return result;
    }

    public async Task<VerifyEmailResult> VerifyEmailAsync(VerifyEmailRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var verification = await FindVerificationAsync(request.Token);
        if (verification == null)
            return new(
                VerifyEmailResultCode.EmailVerificationNotFoundError,
                "This link is not valid"
            );

        var user = await _userRepo.GetAsync(u => u.Id == verification.UserId);
        if (user == null)
            return new(
                VerifyEmailResultCode.EmailVerificationNotFoundError,
                "This link is not valid"
            );

        if (!string.Equals(user.Email, verification.Email, StringComparison.OrdinalIgnoreCase))
            return new(
                VerifyEmailResultCode.EmailChangedError,
                "The email on this account has changed since this link was sent"
            );

        if (verification.CompletedUtc != null)
        {
            return user.EmailVerifiedUtc != null
                ? new(VerifyEmailResultCode.Success, string.Empty, user.Email)
                : new(
                    VerifyEmailResultCode.EmailVerificationAlreadyUsedError,
                    "This link has already been used"
                );
        }

        if (verification.InvalidatedUtc != null)
            return new(
                VerifyEmailResultCode.EmailVerificationInvalidatedError,
                "A newer link has been sent. Use the most recent one"
            );

        var utcNow = Now();
        if (verification.ExpiresUtc <= utcNow)
            return new(
                VerifyEmailResultCode.EmailVerificationExpiredError,
                "This link has expired"
            );

        verification.CompletedUtc = utcNow;
        await _verificationRepo.UpdateAsync(verification);
        await MarkVerifiedAsync(user, EmailProof.Link, keptIdentityId: null, utcNow);

        _logger.LogInformation(
            "Email verification {EmailVerificationId} confirmed for user {UserId}",
            verification.Id,
            user.Id
        );
        return new(VerifyEmailResultCode.Success, string.Empty, user.Email);
    }

    public async Task<GetEmailVerificationStatusResult> GetStatusAsync(string challengeToken)
    {
        var challenge = await FindLiveChallengeAsync(challengeToken);
        if (challenge == null)
            return new(
                GetEmailVerificationStatusResultCode.ChallengeNotFoundError,
                "Sign in again to continue",
                null
            );

        var user = await _userRepo.GetAsync(u => u.Id == challenge.UserId);
        if (user == null)
            return new(
                GetEmailVerificationStatusResultCode.ChallengeNotFoundError,
                "Sign in again to continue",
                null
            );

        var policy = await _securityPolicyProvider.GetPolicyAsync();
        var latest = await LatestLinkAsync(user.Id, user.Email);
        var utcNow = Now();
        var nextResendUtc = latest?.SentUtc.AddSeconds(policy.EmailVerificationResendSeconds);

        var state =
            user.EmailVerifiedUtc != null ? EmailVerificationState.Confirmed
            : latest == null ? EmailVerificationState.NotSent
            : latest.InvalidatedUtc == null && latest.ExpiresUtc > utcNow
                ? EmailVerificationState.Sent
            : EmailVerificationState.Expired;

        return new(
            GetEmailVerificationStatusResultCode.Success,
            string.Empty,
            new EmailVerificationStatus(
                user.Email,
                state,
                latest?.SentUtc,
                latest?.ExpiresUtc,
                nextResendUtc > utcNow ? nextResendUtc : null
            )
        );
    }

    public async Task<string> CreateChallengeAsync(Guid userId, string deviceId, Guid? accountId)
    {
        var user =
            await _userRepo.GetAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException($"User {userId} not found");

        var policy = await _securityPolicyProvider.GetPolicyAsync();
        var utcNow = Now();
        var latest = await LatestLinkAsync(user.Id, user.Email);
        var linkExpiresUtc =
            latest != null
            && latest.InvalidatedUtc == null
            && latest.CompletedUtc == null
            && latest.ExpiresUtc > utcNow
            && latest.SentUtc.AddSeconds(policy.EmailVerificationResendSeconds) > utcNow
                ? latest.ExpiresUtc
                : (await SendLinkAsync(user)).ExpiresUtc
                    ?? utcNow.AddSeconds(policy.EmailVerificationTtlSeconds);

        var secret = _secretProvider.CreateSecret();
        var challenge = new EmailVerificationChallengeEntity
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            SecretHash = Hash(secret),
            DeviceId = deviceId,
            AccountId = accountId,
            ExpiresUtc = ChallengeExpiresUtc(linkExpiresUtc, policy.EmailVerificationTtlSeconds),
        };
        await _challengeRepo.CreateAsync(challenge);

        return new EmailVerificationToken(challenge.Id, secret).ToString();
    }

    public async Task<EmailVerificationChallengeOutcome> CompleteChallengeAsync(
        string challengeToken
    )
    {
        var challenge = await FindLiveChallengeAsync(challengeToken);
        if (challenge == null)
            return new(EmailVerificationChallengeOutcomeCode.NotFound);

        var user = await _userRepo.GetAsync(u => u.Id == challenge.UserId);
        if (user == null)
            return new(EmailVerificationChallengeOutcomeCode.NotFound);

        if (user.EmailVerifiedUtc == null)
            return new(EmailVerificationChallengeOutcomeCode.Pending);

        challenge.CompletedUtc = Now();
        await _challengeRepo.UpdateAsync(challenge);

        return new(
            EmailVerificationChallengeOutcomeCode.Confirmed,
            challenge.UserId,
            challenge.DeviceId,
            challenge.AccountId
        );
    }

    public async Task MarkVerifiedAsync(Guid userId, EmailProof proof, Guid? keptIdentityId = null)
    {
        var user = await _userRepo.GetAsync(u => u.Id == userId);
        if (user != null)
            await MarkVerifiedAsync(user, proof, keptIdentityId, Now());
    }

    public async Task<bool> ReplaceUnconfirmedHolderAsync(string email)
    {
        if (!await _requirement.AppliesAsync())
            return false;

        var holder = await _userRepo.GetAsync(u => u.Email == email);
        if (holder == null || holder.EmailVerifiedUtc != null)
            return false;

        var deleted = await DeleteUnconfirmedAsync(holder.Id);
        if (deleted)
            _logger.LogWarning(
                "Replaced unconfirmed user {UserId} holding an email a new sign up claimed",
                holder.Id
            );
        return deleted;
    }

    public async Task<RejectEmailVerificationResult> RejectAsync(VerifyEmailRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var verification = await FindVerificationAsync(request.Token);
        var user =
            verification == null
                ? null
                : await _userRepo.GetAsync(u => u.Id == verification.UserId);
        if (
            verification == null
            || user == null
            || !string.Equals(user.Email, verification.Email, StringComparison.OrdinalIgnoreCase)
        )
            return new(
                RejectEmailVerificationResultCode.EmailVerificationNotFoundError,
                "This link is not valid"
            );

        if (user.EmailVerifiedUtc != null)
            return new(
                RejectEmailVerificationResultCode.AlreadyConfirmedError,
                "This email is already confirmed. To get back in, reset the password"
            );

        if (
            verification.CompletedUtc != null
            || verification.InvalidatedUtc != null
            || verification.ExpiresUtc <= Now()
        )
            return new(
                RejectEmailVerificationResultCode.EmailVerificationNotFoundError,
                "This link is no longer active. Use the most recent one"
            );

        // Only while confirmation applies is an unconfirmed user sure to have done nothing; on a
        // host without it the user may be in real use, so it is signed out rather than deleted.
        var removed = await _requirement.AppliesAsync() && await DeleteUnconfirmedAsync(user.Id);
        if (!removed)
        {
            var utcNow = Now();
            await InvalidatePendingLinksAsync(user.Id, utcNow);
            await _challengeRepo.ExecuteDeleteAsync(c => c.UserId == user.Id);
            await _authenticationProvider.RevokeAllUserAuthTokensAsync(user.Id);
        }

        _logger.LogWarning(
            "The owner of the email on unconfirmed user {UserId} said they did not sign up",
            user.Id
        );
        return new(RejectEmailVerificationResultCode.Success, string.Empty, removed);
    }

    private async Task<bool> DeleteUnconfirmedAsync(Guid userId)
    {
        var holder = await _userRepo.GetAsync(
            u => u.Id == userId,
            include: q => q.Include(u => u.Accounts).Include(u => u.Groups).Include(u => u.Roles)
        );
        // A user in any account has been put to use, whoever made it, so it is never deleted here.
        if (holder == null || holder.Accounts?.Count > 0)
            return false;

        holder.Accounts?.Clear();
        holder.Groups?.Clear();
        holder.Roles?.Clear();
        await _userRepo.DeleteAsync(holder);
        return true;
    }

    public async Task<bool> MustConfirmEmailAsync(Guid userId) =>
        await _requirement.AppliesAsync()
        && await _userRepo.AnyAsync(u => u.Id == userId && u.EmailVerifiedUtc == null);

    private async Task MarkVerifiedAsync(
        UserEntity user,
        EmailProof proof,
        Guid? keptIdentityId,
        DateTime utcNow
    )
    {
        if (user.EmailVerifiedUtc != null)
            return;

        // Only while confirmation applies is an unconfirmed user one that never proved its address;
        // on a host without it every user is unconfirmed, real ones included.
        if (
            (proof is EmailProof.PasswordRecovery or EmailProof.IdentityProviderClaim)
            && await _requirement.AppliesAsync()
        )
            await TakeOverAsync(user, proof, keptIdentityId);

        user.EmailVerifiedUtc = utcNow;
        await _userRepo.UpdateAsync(user);
        await InvalidatePendingLinksAsync(user.Id, utcNow);
    }

    private async Task TakeOverAsync(UserEntity user, EmailProof proof, Guid? keptIdentityId)
    {
        if (proof != EmailProof.PasswordRecovery)
            await _basicAuthRepo.ExecuteDeleteAsync(b => b.UserId == user.Id);

        await _totpAuthRepo.ExecuteDeleteAsync(t => t.UserId == user.Id);
        await _federatedIdentityRepo.ExecuteDeleteAsync(f =>
            f.UserId == user.Id && (!keptIdentityId.HasValue || f.Id != keptIdentityId.Value)
        );
        await _authenticationProvider.RevokeAllUserAuthTokensAsync(user.Id);

        var utcNow = Now();
        await _passwordRecoveryRepo.ExecuteUpdateAsync(
            r => r.UserId == user.Id && r.CompletedUtc == null && r.InvalidatedUtc == null,
            s => s.SetProperty(r => r.InvalidatedUtc, utcNow)
        );
        await _challengeRepo.ExecuteDeleteAsync(c => c.UserId == user.Id);

        _logger.LogWarning(
            "Took over unconfirmed user {UserId} on proof of its email by {EmailProof}",
            user.Id,
            proof
        );
    }

    private async Task<RequestEmailVerificationResult> SendLinkAsync(UserEntity user)
    {
        if (user.EmailVerifiedUtc != null)
            return new(
                RequestEmailVerificationResultCode.AlreadyVerifiedError,
                "This email is already confirmed"
            );

        var policy = await _securityPolicyProvider.GetPolicyAsync();
        var utcNow = Now();

        var latest = await LatestLinkAsync(user.Id, email: null);
        var nextResendUtc = latest?.SentUtc.AddSeconds(policy.EmailVerificationResendSeconds);
        if (nextResendUtc > utcNow)
            return new(
                RequestEmailVerificationResultCode.ResendCooldownError,
                "A link was sent moments ago. Wait a little before asking for another",
                NextResendUtc: nextResendUtc
            );

        await InvalidatePendingLinksAsync(user.Id, utcNow);

        var secret = _secretProvider.CreateSecret();
        var verification = new EmailVerificationEntity
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            Email = user.Email,
            SecretHash = Hash(secret),
            ExpiresUtc = utcNow.AddSeconds(policy.EmailVerificationTtlSeconds),
            SentUtc = utcNow,
        };
        await _verificationRepo.CreateAsync(verification);

        var token = new EmailVerificationToken(verification.Id, secret).ToString();
        var (delivery, deliveryMessage) = await DeliverAsync(
            new EmailVerificationDelivery(
                verification.Id,
                user.Id,
                user.Username,
                user.Email,
                verification.ExpiresUtc,
                token
            )
        );

        _logger.LogInformation(
            "Created email verification {EmailVerificationId} for user {UserId}: {Delivery}",
            verification.Id,
            user.Id,
            delivery
        );

        return new(
            RequestEmailVerificationResultCode.Success,
            string.Empty,
            delivery == EmailVerificationDeliveryStatus.Sent ? null : token,
            verification.ExpiresUtc,
            utcNow.AddSeconds(policy.EmailVerificationResendSeconds),
            delivery,
            deliveryMessage
        );
    }

    private async Task<(EmailVerificationDeliveryStatus Status, string? Message)> DeliverAsync(
        EmailVerificationDelivery delivery
    )
    {
        if (_requirement.Sender == null)
            return (EmailVerificationDeliveryStatus.NotAttempted, null);

        try
        {
            var result = await _requirement.Sender.SendAsync(delivery);
            return result.ResultCode == EmailVerificationSendResultCode.Sent
                ? (EmailVerificationDeliveryStatus.Sent, result.Message)
                : (EmailVerificationDeliveryStatus.Failed, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Email verification sender failed for verification {EmailVerificationId}",
                delivery.VerificationId
            );
            return (EmailVerificationDeliveryStatus.Failed, null);
        }
    }

    private Task<EmailVerificationEntity?> LatestLinkAsync(Guid userId, string? email) =>
        _verificationRepo.GetAsync(
            v => v.UserId == userId && (email == null || v.Email == email),
            orderBy: q => q.OrderByDescending(v => v.SentUtc)
        );

    private Task InvalidatePendingLinksAsync(Guid userId, DateTime utcNow) =>
        _verificationRepo.ExecuteUpdateAsync(
            v => v.UserId == userId && v.CompletedUtc == null && v.InvalidatedUtc == null,
            s => s.SetProperty(v => v.InvalidatedUtc, utcNow)
        );

    private async Task<EmailVerificationEntity?> FindVerificationAsync(string? token)
    {
        if (!EmailVerificationToken.TryParse(token, out var parsed))
            return null;

        var verification = await _verificationRepo.GetAsync(v => v.Id == parsed.Id);
        return verification != null && Verify(verification.SecretHash, parsed.Secret)
            ? verification
            : null;
    }

    private async Task<EmailVerificationChallengeEntity?> FindLiveChallengeAsync(string? token)
    {
        if (!EmailVerificationToken.TryParse(token, out var parsed))
            return null;

        var challenge = await _challengeRepo.GetAsync(c => c.Id == parsed.Id);
        return
            challenge != null
            && challenge.CompletedUtc == null
            && challenge.ExpiresUtc > Now()
            && Verify(challenge.SecretHash, parsed.Secret)
            ? challenge
            : null;
    }

    private async Task ExtendChallengeAsync(
        EmailVerificationChallengeEntity challenge,
        DateTime linkExpiresUtc
    )
    {
        var policy = await _securityPolicyProvider.GetPolicyAsync();
        challenge.ExpiresUtc = ChallengeExpiresUtc(
            linkExpiresUtc,
            policy.EmailVerificationTtlSeconds
        );
        await _challengeRepo.UpdateAsync(challenge);
    }

    internal static DateTime ChallengeExpiresUtc(DateTime linkExpiresUtc, int ttlSeconds) =>
        linkExpiresUtc.AddSeconds(ttlSeconds);

    private string Hash(string secret) =>
        secret
            .ToHashedValueFromPlainText(_hashProviderFactory, _hashCodes.TokenHashCode)
            .ToHashString()!;

    private bool Verify(string storedHash, string secret) =>
        storedHash.ToHashedValue(_hashProviderFactory).Verify(secret);

    private DateTime Now() => _timeProvider.GetUtcNow().UtcDateTime;
}
