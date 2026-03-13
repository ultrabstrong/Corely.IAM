using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.GoogleAuths.Entities;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.GoogleAuths.Providers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.GoogleAuths.Processors;

internal class GoogleAuthProcessor(
    IRepo<GoogleAuthEntity> googleAuthRepo,
    IReadonlyRepo<BasicAuthEntity> basicAuthRepo,
    IGoogleIdTokenValidator googleIdTokenValidator,
    ILogger<GoogleAuthProcessor> logger
) : IGoogleAuthProcessor
{
    private readonly IRepo<GoogleAuthEntity> _googleAuthRepo = googleAuthRepo.ThrowIfNull(
        nameof(googleAuthRepo)
    );
    private readonly IReadonlyRepo<BasicAuthEntity> _basicAuthRepo = basicAuthRepo.ThrowIfNull(
        nameof(basicAuthRepo)
    );
    private readonly IGoogleIdTokenValidator _googleIdTokenValidator =
        googleIdTokenValidator.ThrowIfNull(nameof(googleIdTokenValidator));
    private readonly ILogger<GoogleAuthProcessor> _logger = logger.ThrowIfNull(nameof(logger));

    public async Task<LinkGoogleAuthResult> LinkGoogleAuthAsync(Guid userId, string googleIdToken)
    {
        var payload = await _googleIdTokenValidator.ValidateAsync(googleIdToken);
        if (payload == null)
        {
            return new LinkGoogleAuthResult(
                LinkGoogleAuthResultCode.InvalidGoogleTokenError,
                "Invalid Google ID token"
            );
        }

        var existing = await _googleAuthRepo.GetAsync(e => e.UserId == userId);
        if (existing != null)
        {
            return new LinkGoogleAuthResult(
                LinkGoogleAuthResultCode.AlreadyLinkedError,
                "A Google account is already linked"
            );
        }

        var subjectInUse = await _googleAuthRepo.GetAsync(e =>
            e.GoogleSubjectId == payload.Subject
        );
        if (subjectInUse != null)
        {
            return new LinkGoogleAuthResult(
                LinkGoogleAuthResultCode.GoogleAccountInUseError,
                "This Google account is already linked to another user"
            );
        }

        var entity = new GoogleAuthEntity
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            GoogleSubjectId = payload.Subject,
            Email = payload.Email,
        };
        await _googleAuthRepo.CreateAsync(entity);

        _logger.LogDebug(
            "Google account linked for UserId {UserId} (Subject: {Subject})",
            userId,
            payload.Subject
        );

        return new LinkGoogleAuthResult(LinkGoogleAuthResultCode.Success, string.Empty);
    }

    public async Task<UnlinkGoogleAuthResult> UnlinkGoogleAuthAsync(Guid userId)
    {
        var googleAuth = await _googleAuthRepo.GetAsync(e => e.UserId == userId);
        if (googleAuth == null)
        {
            return new UnlinkGoogleAuthResult(
                UnlinkGoogleAuthResultCode.NotLinkedError,
                "No Google account linked"
            );
        }

        var hasBasicAuth = await _basicAuthRepo.GetAsync(e => e.UserId == userId) != null;
        if (!hasBasicAuth)
        {
            return new UnlinkGoogleAuthResult(
                UnlinkGoogleAuthResultCode.LastAuthMethodError,
                "Cannot unlink the only authentication method"
            );
        }

        await _googleAuthRepo.DeleteAsync(googleAuth);

        _logger.LogDebug("Google account unlinked for UserId {UserId}", userId);

        return new UnlinkGoogleAuthResult(UnlinkGoogleAuthResultCode.Success, string.Empty);
    }

    public async Task<AuthMethodsResult> GetAuthMethodsAsync(Guid userId)
    {
        var basicAuth = await _basicAuthRepo.GetAsync(e => e.UserId == userId);
        var googleAuth = await _googleAuthRepo.GetAsync(e => e.UserId == userId);

        return new AuthMethodsResult(
            AuthMethodsResultCode.Success,
            string.Empty,
            HasBasicAuth: basicAuth != null,
            HasGoogleAuth: googleAuth != null,
            GoogleEmail: googleAuth?.Email
        );
    }

    public async Task<Guid?> GetUserIdByGoogleSubjectAsync(string googleSubjectId)
    {
        var entity = await _googleAuthRepo.GetAsync(e => e.GoogleSubjectId == googleSubjectId);
        return entity?.UserId;
    }
}
