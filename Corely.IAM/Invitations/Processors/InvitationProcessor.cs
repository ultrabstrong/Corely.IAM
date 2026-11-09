using System.Linq.Expressions;
using Corely.Common.Extensions;
using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Invitations.Entities;
using Corely.IAM.Invitations.Enums;
using Corely.IAM.Invitations.Mappers;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Invitations.Providers;
using Corely.IAM.Models;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using Corely.Security.Hashing.Factories;
using Corely.Security.Secrets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Invitations.Processors;

internal class InvitationProcessor(
    IRepo<InvitationEntity> invitationRepo,
    IRepo<AccountEntity> accountRepo,
    IAccountProcessor accountProcessor,
    IUserContextProvider userContextProvider,
    IValidationProvider validationProvider,
    IHashProviderFactory hashProviderFactory,
    IamHashCodes hashCodes,
    ISecretProvider secretProvider,
    TimeProvider timeProvider,
    ILogger<InvitationProcessor> logger,
    IInvitationSender? invitationSender = null
) : IInvitationProcessor
{
    private readonly IHashProviderFactory _hashProviderFactory = hashProviderFactory.ThrowIfNull(
        nameof(hashProviderFactory)
    );
    private readonly IamHashCodes _hashCodes = hashCodes.ThrowIfNull(nameof(hashCodes));
    private readonly ISecretProvider _secretProvider = secretProvider.ThrowIfNull(
        nameof(secretProvider)
    );
    private readonly IRepo<InvitationEntity> _invitationRepo = invitationRepo.ThrowIfNull(
        nameof(invitationRepo)
    );
    private readonly IRepo<AccountEntity> _accountRepo = accountRepo.ThrowIfNull(
        nameof(accountRepo)
    );
    private readonly IAccountProcessor _accountProcessor = accountProcessor.ThrowIfNull(
        nameof(accountProcessor)
    );
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );
    private readonly IValidationProvider _validationProvider = validationProvider.ThrowIfNull(
        nameof(validationProvider)
    );
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));
    private readonly ILogger<InvitationProcessor> _logger = logger.ThrowIfNull(nameof(logger));
    private readonly IInvitationSender? _invitationSender = invitationSender;

    public async Task<CreateInvitationResult> CreateInvitationAsync(CreateInvitationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var validation = _validationProvider.ValidateAndLog(request);
        if (!validation.IsValid)
        {
            return new CreateInvitationResult(
                CreateInvitationResultCode.ValidationError,
                validation.Message,
                null,
                null
            );
        }

        var accountCheck = await _accountRepo.EvaluateAsync(
            async (q, ct) =>
                await q.Where(a => a.Id == request.AccountId)
                    .Select(a => new
                    {
                        a.AccountName,
                        ExistingMemberId = a.Users!.Where(u => u.Email == request.Email)
                            .Select(u => (Guid?)u.Id)
                            .FirstOrDefault(),
                    })
                    .FirstOrDefaultAsync(ct)
        );

        if (accountCheck == null)
        {
            _logger.LogWarning("Account with Id {AccountId} not found", request.AccountId);
            return new CreateInvitationResult(
                CreateInvitationResultCode.AccountNotFoundError,
                $"Account with Id {request.AccountId} not found",
                null,
                null
            );
        }

        if (accountCheck.ExistingMemberId != null)
        {
            _logger.LogWarning(
                "User {UserId} with email {Email} is already a member of account {AccountId}",
                accountCheck.ExistingMemberId,
                request.Email,
                request.AccountId
            );
            return new CreateInvitationResult(
                CreateInvitationResultCode.UserAlreadyInAccountError,
                $"A user with email '{request.Email}' is already a member of this account.",
                null,
                null,
                accountCheck.ExistingMemberId
            );
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        if (GetRequiredUserContext("create invitations").User is not { } inviter)
        {
            return new CreateInvitationResult(
                CreateInvitationResultCode.UnauthorizedError,
                "Only a user can create an invitation",
                null,
                null
            );
        }

        var secret = _secretProvider.CreateSecret();
        var entity = new InvitationEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = request.AccountId,
            SecretHash = secret
                .ToHashedValueFromPlainText(_hashProviderFactory, _hashCodes.TokenHashCode)
                .ToHashString()!,
            CreatedByUserId = inviter.Id,
            Email = request.Email,
            Description = request.Description,
            ExpiresUtc = utcNow.AddSeconds(request.ExpiresInSeconds),
        };

        await _invitationRepo.CreateAsync(entity);

        _logger.LogInformation(
            "Invitation {InvitationId} created for account {AccountId}",
            entity.Id,
            request.AccountId
        );

        var token = new InvitationToken(entity.Id, secret).ToString();
        var (delivery, deliveryMessage) = await SendAsync(
            new InvitationDelivery(
                entity.Id,
                entity.AccountId,
                accountCheck.AccountName,
                entity.Email,
                entity.Description,
                entity.ExpiresUtc,
                inviter.Username,
                inviter.Email,
                token
            )
        );

        return new CreateInvitationResult(
            CreateInvitationResultCode.Success,
            string.Empty,
            delivery == InvitationDeliveryStatus.Sent ? null : token,
            entity.Id,
            ExpiresUtc: entity.ExpiresUtc,
            Delivery: delivery,
            DeliveryMessage: deliveryMessage
        );
    }

    private async Task<(InvitationDeliveryStatus Status, string? Message)> SendAsync(
        InvitationDelivery delivery
    )
    {
        if (_invitationSender == null)
            return (InvitationDeliveryStatus.NotAttempted, null);

        try
        {
            var result = await _invitationSender.SendAsync(delivery);
            return result.ResultCode == InvitationSendResultCode.Sent
                ? (
                    InvitationDeliveryStatus.Sent,
                    result.Message ?? $"Invitation sent to {delivery.Email}."
                )
                : (InvitationDeliveryStatus.Failed, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Invitation sender failed for invitation {InvitationId}",
                delivery.InvitationId
            );
            return (InvitationDeliveryStatus.Failed, null);
        }
    }

    public async Task<AcceptInvitationResult> AcceptInvitationAsync(AcceptInvitationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        if (!InvitationToken.TryParse(request.Token, out var invitationToken))
        {
            return new AcceptInvitationResult(
                AcceptInvitationResultCode.InvitationNotFoundError,
                "Invitation not found",
                null
            );
        }

        var invitationId = invitationToken.InvitationId;
        var invitationEntity = await _invitationRepo.GetAsync(i => i.Id == invitationId);
        if (
            invitationEntity == null
            || !invitationEntity
                .SecretHash.ToHashedValue(_hashProviderFactory)
                .Verify(invitationToken.Secret)
        )
        {
            _logger.LogWarning("Invitation not found for provided token");
            return new AcceptInvitationResult(
                AcceptInvitationResultCode.InvitationNotFoundError,
                "Invitation not found",
                null
            );
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        if (invitationEntity.ExpiresUtc < utcNow)
        {
            return new AcceptInvitationResult(
                AcceptInvitationResultCode.InvitationExpiredError,
                "Invitation has expired",
                null
            );
        }

        if (invitationEntity.RevokedUtc != null)
        {
            return new AcceptInvitationResult(
                AcceptInvitationResultCode.InvitationRevokedError,
                "Invitation has been revoked",
                null
            );
        }

        if (invitationEntity.AcceptedByUserId != null)
        {
            return new AcceptInvitationResult(
                AcceptInvitationResultCode.InvitationAlreadyAcceptedError,
                "Invitation has already been accepted",
                null
            );
        }

        var userContext = GetRequiredUserContext("accept invitations");
        var userId = userContext.User!.Id;

        if (
            !string.Equals(
                userContext.User!.Email,
                invitationEntity.Email,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            _logger.LogWarning(
                "User {UserId} attempted to accept invitation {InvitationId} intended for {InvitedEmail}",
                userId,
                invitationEntity.Id,
                invitationEntity.Email
            );
            return new AcceptInvitationResult(
                AcceptInvitationResultCode.EmailMismatchError,
                "This invitation is for a different user",
                null
            );
        }

        var addResult = await _accountProcessor.AddUserToAccountForInvitationAsync(
            new AddUserToAccountRequest(userId, invitationEntity.AccountId)
        );

        if (
            addResult.ResultCode != AddUserToAccountResultCode.Success
            && addResult.ResultCode != AddUserToAccountResultCode.UserAlreadyInAccountError
        )
        {
            return new AcceptInvitationResult(
                AcceptInvitationResultCode.AddToAccountError,
                addResult.Message,
                null
            );
        }

        invitationEntity.AcceptedByUserId = userId;
        invitationEntity.AcceptedUtc = utcNow;
        await _invitationRepo.UpdateAsync(invitationEntity);

        var siblingInvitations = await _invitationRepo.ListAsync(i =>
            i.AccountId == invitationEntity.AccountId
            && i.Email == invitationEntity.Email
            && i.Id != invitationEntity.Id
            && i.AcceptedByUserId == null
            && i.RevokedUtc == null
        );
        foreach (var sibling in siblingInvitations)
        {
            sibling.AcceptedByUserId = userId;
            sibling.AcceptedUtc = utcNow;
            await _invitationRepo.UpdateAsync(sibling);
        }

        _logger.LogInformation(
            "User {UserId} accepted invitation {InvitationId} for account {AccountId}",
            userId,
            invitationEntity.Id,
            invitationEntity.AccountId
        );

        return new AcceptInvitationResult(
            AcceptInvitationResultCode.Success,
            string.Empty,
            invitationEntity.AccountId
        );
    }

    public async Task<RevokeInvitationResult> RevokeInvitationAsync(RevokeInvitationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var invitationEntity = await _invitationRepo.GetAsync(
            AccountScope.Of(request.AccountId).ByIdPredicate<InvitationEntity>(request.InvitationId)
        );
        if (invitationEntity == null)
        {
            _logger.LogWarning(
                "Invitation with Id {InvitationId} not found for account {AccountId}",
                request.InvitationId,
                request.AccountId
            );
            return new RevokeInvitationResult(
                RevokeInvitationResultCode.InvitationNotFoundError,
                $"Invitation with Id {request.InvitationId} not found for account {request.AccountId}"
            );
        }

        if (invitationEntity.AcceptedByUserId != null)
        {
            return new RevokeInvitationResult(
                RevokeInvitationResultCode.InvitationAlreadyAcceptedError,
                "Invitation has already been accepted and cannot be revoked"
            );
        }

        invitationEntity.RevokedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        await _invitationRepo.UpdateAsync(invitationEntity);

        _logger.LogInformation(
            "Invitation {InvitationId} revoked for account {AccountId}",
            request.InvitationId,
            request.AccountId
        );
        return new RevokeInvitationResult(RevokeInvitationResultCode.Success, string.Empty);
    }

    public async Task<ListResult<Invitation>> ListInvitationsAsync(
        ListInvitationsRequest request,
        IReadOnlySet<Guid>? authorizedResourceIds = null
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        Expression<Func<InvitationEntity, bool>> scopePredicate = request.StatusFilter switch
        {
            InvitationStatus.Pending => i =>
                i.AccountId == request.AccountId
                && i.AcceptedByUserId == null
                && i.RevokedUtc == null
                && i.ExpiresUtc >= utcNow,
            InvitationStatus.Accepted => i =>
                i.AccountId == request.AccountId && i.AcceptedByUserId != null,
            InvitationStatus.Revoked => i =>
                i.AccountId == request.AccountId && i.RevokedUtc != null,
            InvitationStatus.Expired => i =>
                i.AccountId == request.AccountId
                && i.AcceptedByUserId == null
                && i.RevokedUtc == null
                && i.ExpiresUtc < utcNow,
            _ => i => i.AccountId == request.AccountId,
        };

        return await ListQueryHelper.ExecuteListAsync(
            _invitationRepo,
            scopePredicate,
            request.Filter,
            request.Order,
            request.Skip,
            request.Take,
            e => e.ToModel(),
            authorizedResourceIds
        );
    }

    private UserContext GetRequiredUserContext(string operation)
    {
        var userContext = _userContextProvider.GetUserContext();
        if (userContext?.User == null)
            throw new InvalidOperationException(
                $"A non-system user context is required to {operation}."
            );

        return userContext;
    }
}
