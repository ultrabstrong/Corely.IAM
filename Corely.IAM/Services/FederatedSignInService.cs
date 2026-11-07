using System.Security.Cryptography;
using System.Text;
using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Corely.IAM.Extensions;
using Corely.IAM.FederatedIdentities.Models;
using Corely.IAM.FederatedIdentities.Processors;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;
using Corely.IAM.IdentityProviders.Providers;
using Corely.IAM.IdentityProviders.Types;
using Corely.IAM.Models;
using Corely.IAM.Security.Entities;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class FederatedSignInService(
    IIdentityProviderSettingsProvider identityProviderSettingsProvider,
    IdentityProviderCatalog catalog,
    IFederationClient federationClient,
    IFederatedIdentityProcessor federatedIdentityProcessor,
    IUserProcessor userProcessor,
    IReadonlyRepo<UserEntity> userRepo,
    IReadonlyRepo<AuthTokenEntity> authTokenRepo,
    AuthenticationService authenticationService,
    IUserContextProvider userContextProvider,
    IUnitOfWorkProvider uowProvider,
    TimeProvider timeProvider,
    ILogger<FederatedSignInService> logger
) : IFederatedSignInService
{
    public const int MAX_USERNAME_ATTEMPTS = 10;

    private readonly IIdentityProviderSettingsProvider _identityProviderSettingsProvider =
        identityProviderSettingsProvider.ThrowIfNull(nameof(identityProviderSettingsProvider));
    private readonly IdentityProviderCatalog _catalog = catalog.ThrowIfNull(nameof(catalog));
    private readonly IFederationClient _federationClient = federationClient.ThrowIfNull(
        nameof(federationClient)
    );
    private readonly IFederatedIdentityProcessor _federatedIdentityProcessor =
        federatedIdentityProcessor.ThrowIfNull(nameof(federatedIdentityProcessor));
    private readonly IUserProcessor _userProcessor = userProcessor.ThrowIfNull(
        nameof(userProcessor)
    );
    private readonly IReadonlyRepo<UserEntity> _userRepo = userRepo.ThrowIfNull(nameof(userRepo));
    private readonly IReadonlyRepo<AuthTokenEntity> _authTokenRepo = authTokenRepo.ThrowIfNull(
        nameof(authTokenRepo)
    );
    private readonly AuthenticationService _authenticationService =
        authenticationService.ThrowIfNull(nameof(authenticationService));
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );
    private readonly IUnitOfWorkProvider _uowProvider = uowProvider.ThrowIfNull(
        nameof(uowProvider)
    );
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));
    private readonly ILogger<FederatedSignInService> _logger = logger.ThrowIfNull(nameof(logger));

    public async Task<IReadOnlyList<IdentityProviderOption>> ListIdentityProvidersAsync() =>
        [
            .. (await _identityProviderSettingsProvider.GetAllAsync())
                .Where(s => s.IsUsable)
                .OrderBy(s => s.Type)
                .Select(s => new IdentityProviderOption(
                    s.Type,
                    _catalog.Get(s.Type).DisplayName,
                    s.AllowSignUp
                )),
        ];

    public async Task<StartFederatedSignInResult> StartFederatedSignInAsync(
        StartFederatedSignInRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        if (request.Purpose != FederatedSignInPurpose.SignIn && CurrentUserId() is null)
        {
            return new(
                StartFederatedSignInResultCode.UnauthorizedError,
                "Linking and proving it is you need a signed in user",
                null,
                null
            );
        }

        var provider = await _identityProviderSettingsProvider.GetUsableAsync(request.Type);
        if (provider is null)
        {
            return new(
                StartFederatedSignInResultCode.ProviderNotAvailableError,
                $"{request.Type} sign in is not available",
                null,
                null
            );
        }

        var authorization = await _federationClient.AuthorizeAsync(
            _catalog.Get(request.Type),
            provider,
            request.Purpose,
            request.RedirectUri,
            UtcNow()
        );
        return authorization is null
            ? new(
                StartFederatedSignInResultCode.ProviderUnreachableError,
                $"{request.Type} could not be reached. Try again shortly.",
                null,
                null
            )
            : new(
                StartFederatedSignInResultCode.Success,
                string.Empty,
                authorization.Url,
                authorization.Correlation
            );
    }

    public async Task<CompleteFederatedSignInResult> CompleteFederatedSignInAsync(
        CompleteFederatedSignInRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        var correlation = request.Correlation;
        var now = UtcNow();

        if (correlation.HasExpired(now))
            return Failed(
                request,
                CompleteFederatedSignInResultCode.ExpiredError,
                "This sign in took too long. Start again."
            );

        if (!Matches(request.State, correlation.State))
            return Failed(
                request,
                CompleteFederatedSignInResultCode.InvalidStateError,
                "This sign in could not be matched to one started here. Start again."
            );

        if (request.Issuer is not null && correlation.Issuer != request.Issuer)
            return Failed(
                request,
                CompleteFederatedSignInResultCode.IssuerMismatchError,
                "The response came from a different provider than the one this sign in started with."
            );

        if (!string.IsNullOrWhiteSpace(request.Error))
            return Failed(
                request,
                CompleteFederatedSignInResultCode.ProviderDeclinedError,
                $"{correlation.Type} did not complete the sign in."
            );

        if (string.IsNullOrWhiteSpace(request.Code))
            return Failed(
                request,
                CompleteFederatedSignInResultCode.InvalidResponseError,
                $"{correlation.Type} returned no authorization code."
            );

        var provider = await _identityProviderSettingsProvider.GetUsableAsync(correlation.Type);
        if (provider is null)
            return Failed(
                request,
                CompleteFederatedSignInResultCode.ProviderNotAvailableError,
                $"{correlation.Type} sign in is not available"
            );

        var identity = await _federationClient.RedeemAsync(
            _catalog.Get(correlation.Type),
            provider,
            correlation,
            request.Code,
            request.RedirectUri,
            now
        );
        if (identity is null)
            return Failed(
                request,
                CompleteFederatedSignInResultCode.InvalidResponseError,
                $"{correlation.Type} sign in could not be verified. Start again."
            );

        return correlation.Purpose switch
        {
            FederatedSignInPurpose.Link => await LinkAsync(request, identity),
            FederatedSignInPurpose.Reauthenticate => await ReauthenticateAsync(
                request,
                identity,
                now
            ),
            _ => await SignInAsync(request, provider.Settings, identity),
        };
    }

    private async Task<CompleteFederatedSignInResult> SignInAsync(
        CompleteFederatedSignInRequest request,
        IdentityProviderSettings settings,
        ExternalIdentity identity
    )
    {
        var userId = await _federatedIdentityProcessor.FindUserIdAsync(
            identity.Issuer,
            identity.Subject
        );
        var createdUser = false;
        if (userId is null)
        {
            var signUp = await SignUpAsync(request, settings, identity);
            if (signUp.Failure is not null || signUp.UserId is not { } newUserId)
                return signUp.Failure!;
            userId = newUserId;
            createdUser = true;
        }

        var signIn = await _authenticationService.SignInVerifiedUserAsync(
            userId.Value,
            request.DeviceId,
            request.AccountId
        );
        var code = signIn.ResultCode switch
        {
            SignInResultCode.Success => CompleteFederatedSignInResultCode.Success,
            SignInResultCode.MfaRequiredChallenge =>
                CompleteFederatedSignInResultCode.MfaRequiredChallenge,
            SignInResultCode.UserLockedError => CompleteFederatedSignInResultCode.UserLockedError,
            _ => CompleteFederatedSignInResultCode.SignInError,
        };
        return new(
            code,
            signIn.Message ?? string.Empty,
            identity.Type,
            request.Purpose,
            signIn,
            createdUser
        );
    }

    private async Task<(Guid? UserId, CompleteFederatedSignInResult? Failure)> SignUpAsync(
        CompleteFederatedSignInRequest request,
        IdentityProviderSettings settings,
        ExternalIdentity identity
    )
    {
        var displayName = _catalog.Get(identity.Type).DisplayName;
        if (!settings.AllowSignUp)
            return (
                null,
                Failed(
                    request,
                    CompleteFederatedSignInResultCode.NotLinkedError,
                    $"No account is linked to this {displayName} account. Sign in another way and link {displayName} from your profile."
                )
            );

        if (!identity.HasEmail)
            return (
                null,
                Failed(
                    request,
                    CompleteFederatedSignInResultCode.NoEmailError,
                    $"{displayName} shared no email address, so an account cannot be created from it. Sign up another way and link {displayName} from your profile."
                )
            );

        if (!identity.EmailVerified)
            return (
                null,
                Failed(
                    request,
                    CompleteFederatedSignInResultCode.EmailNotVerifiedError,
                    $"{displayName} has not verified this email address, so an account cannot be created from it."
                )
            );

        if (await _userRepo.AnyAsync(u => u.Email == identity.Email))
            return (
                null,
                Failed(
                    request,
                    CompleteFederatedSignInResultCode.EmailInUseError,
                    $"An account already uses this email. Sign in and link {displayName} from your profile."
                )
            );

        var committed = false;
        try
        {
            await _uowProvider.BeginAsync();

            CreateUserResult? created = null;
            for (var attempt = 0; attempt < MAX_USERNAME_ATTEMPTS; attempt++)
            {
                created = await _userProcessor.CreateUserAsync(
                    new(identity.Email!.UsernameCandidate(attempt), identity.Email!)
                );
                if (created.ResultCode != CreateUserResultCode.UserExistsError)
                    break;
            }

            if (created?.ResultCode != CreateUserResultCode.Success)
            {
                _logger.LogInformation(
                    "Could not create a user from a {Type} identity: {Message}",
                    identity.Type,
                    created?.Message
                );
                return (
                    null,
                    Failed(
                        request,
                        CompleteFederatedSignInResultCode.SignInError,
                        "An account could not be created. Try again."
                    )
                );
            }

            var linked = await _federatedIdentityProcessor.LinkToNewUserAsync(
                created.CreatedId,
                identity
            );
            if (linked.ResultCode != LinkFederatedIdentityResultCode.Success)
                return (
                    null,
                    Failed(
                        request,
                        CompleteFederatedSignInResultCode.IdentityInUseError,
                        linked.Message
                    )
                );

            await _uowProvider.CommitAsync();
            committed = true;
            _logger.LogInformation(
                "User {UserId} signed up with a {Type} identity",
                created.CreatedId,
                identity.Type
            );
            return (created.CreatedId, null);
        }
        finally
        {
            if (!committed)
                await _uowProvider.RollbackAsync();
        }
    }

    private async Task<CompleteFederatedSignInResult> LinkAsync(
        CompleteFederatedSignInRequest request,
        ExternalIdentity identity
    )
    {
        if (CurrentUserId() is not { } userId)
            return Failed(
                request,
                CompleteFederatedSignInResultCode.UnauthorizedError,
                "Sign in before linking an account."
            );

        var linked = await _federatedIdentityProcessor.LinkAsync(userId, identity);
        return linked.ResultCode switch
        {
            LinkFederatedIdentityResultCode.Success => new(
                CompleteFederatedSignInResultCode.Success,
                string.Empty,
                identity.Type,
                request.Purpose
            ),
            LinkFederatedIdentityResultCode.AlreadyLinkedError => Failed(
                request,
                CompleteFederatedSignInResultCode.AlreadyLinkedError,
                linked.Message
            ),
            LinkFederatedIdentityResultCode.IdentityInUseError => Failed(
                request,
                CompleteFederatedSignInResultCode.IdentityInUseError,
                linked.Message
            ),
            _ => Failed(
                request,
                CompleteFederatedSignInResultCode.UnauthorizedError,
                linked.Message
            ),
        };
    }

    private async Task<CompleteFederatedSignInResult> ReauthenticateAsync(
        CompleteFederatedSignInRequest request,
        ExternalIdentity identity,
        DateTime now
    )
    {
        if (CurrentUserId() is not { } userId || await CurrentSessionIdAsync() is not { } sessionId)
            return Failed(
                request,
                CompleteFederatedSignInResultCode.UnauthorizedError,
                "Sign in before proving it is you."
            );

        var definition = _catalog.Get(identity.Type);
        if (
            definition
                is OpenIdConnectProviderDefinition { ForcesFreshSignInForReauthentication: true }
            && (
                identity.AuthTimeUtc is not { } authTime
                || authTime
                    < now.AddSeconds(
                        -FederatedIdentities
                            .Entities
                            .FederatedIdentityEntity
                            .REAUTHENTICATION_MAX_AGE_SECONDS - FederationClient.CLOCK_SKEW_SECONDS
                    )
            )
        )
        {
            return Failed(
                request,
                CompleteFederatedSignInResultCode.InvalidResponseError,
                $"{definition.DisplayName} did not ask for your credentials again. Try again."
            );
        }

        return await _federatedIdentityProcessor.RecordReauthenticationAsync(
            userId,
            identity,
            sessionId
        )
            ? new(
                CompleteFederatedSignInResultCode.Success,
                string.Empty,
                identity.Type,
                request.Purpose
            )
            : Failed(
                request,
                CompleteFederatedSignInResultCode.NotLinkedToUserError,
                $"Sign in with the {definition.DisplayName} account linked to you."
            );
    }

    private Guid? CurrentUserId() =>
        _userContextProvider.GetUserContext() is { IsSystemContext: false, User: { } user }
            ? user.Id
            : null;

    private async Task<Guid?> CurrentSessionIdAsync() =>
        _userContextProvider.GetUserContext()?.AuthTokenId is { } tokenId
            ? (await _authTokenRepo.GetAsync(t => t.Id == tokenId))?.SessionId
            : null;

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    private static bool Matches(string? received, string expected) =>
        received is not null
        && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(received),
            Encoding.UTF8.GetBytes(expected)
        );

    private static CompleteFederatedSignInResult Failed(
        CompleteFederatedSignInRequest request,
        CompleteFederatedSignInResultCode code,
        string message
    ) => new(code, message, request.Type, request.Purpose);
}
