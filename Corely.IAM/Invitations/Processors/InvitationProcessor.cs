using System.Linq.Expressions;
using System.Security.Cryptography;
using Corely.Common.Extensions;
using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Invitations.Constants;
using Corely.IAM.Invitations.Entities;
using Corely.IAM.Invitations.Mappers;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Models;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Invitations.Processors;

internal class InvitationProcessor(
    IRepo<InvitationEntity> invitationRepo,
    IRepo<AccountEntity> accountRepo,
    IReadonlyRepo<UserEntity> userRepo,
    IUserContextProvider userContextProvider,
    IValidationProvider validationProvider,
    TimeProvider timeProvider,
    ILogger<InvitationProcessor> logger
) : IInvitationProcessor
{
    private readonly IRepo<InvitationEntity> _invitationRepo = invitationRepo.ThrowIfNull(
        nameof(invitationRepo)
    );
    private readonly IRepo<AccountEntity> _accountRepo = accountRepo.ThrowIfNull(
        nameof(accountRepo)
    );
    private readonly IReadonlyRepo<UserEntity> _userRepo = userRepo.ThrowIfNull(nameof(userRepo));
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );
    private readonly IValidationProvider _validationProvider = validationProvider.ThrowIfNull(
        nameof(validationProvider)
    );
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));
    private readonly ILogger<InvitationProcessor> _logger = logger.ThrowIfNull(nameof(logger));

    public async Task<CreateInvitationResult> CreateInvitationAsync(CreateInvitationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        try
        {
            _validationProvider.ThrowIfInvalid(request);
        }
        catch (IAM.Validators.ValidationException ex)
        {
            return new CreateInvitationResult(
                CreateInvitationResultCode.ValidationError,
                ex.Message,
                null,
                null
            );
        }

        var accountEntity = await _accountRepo.GetAsync(a => a.Id == request.AccountId);
        if (accountEntity == null)
        {
            _logger.LogWarning("Account with Id {AccountId} not found", request.AccountId);
            return new CreateInvitationResult(
                CreateInvitationResultCode.AccountNotFoundError,
                $"Account with Id {request.AccountId} not found",
                null,
                null
            );
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var userId = _userContextProvider.GetUserContext()!.User.Id;

        var tokenBytes = RandomNumberGenerator.GetBytes(InvitationConstants.TOKEN_LENGTH);
        var token = Convert
            .ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var entity = new InvitationEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = request.AccountId,
            Token = token,
            CreatedByUserId = userId,
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

        return new CreateInvitationResult(
            CreateInvitationResultCode.Success,
            string.Empty,
            token,
            entity.Id
        );
    }

    public async Task<AcceptInvitationResult> AcceptInvitationAsync(AcceptInvitationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return new AcceptInvitationResult(
                AcceptInvitationResultCode.InvitationNotFoundError,
                "Token is required",
                null
            );
        }

        var invitationEntity = await _invitationRepo.GetAsync(i => i.Token == request.Token);
        if (invitationEntity == null)
        {
            _logger.LogInformation("Invitation not found for provided token");
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

        var userId = _userContextProvider.GetUserContext()!.User.Id;

        // Mark this invitation as accepted
        invitationEntity.AcceptedByUserId = userId;
        invitationEntity.AcceptedUtc = utcNow;
        await _invitationRepo.UpdateAsync(invitationEntity);

        // Bulk-burn sibling invitations for same account + email
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

        // Check if user is already in the account
        var accountEntity = await _accountRepo.GetAsync(
            a => a.Id == invitationEntity.AccountId,
            include: q => q.Include(a => a.Users)
        );

        if (accountEntity == null)
        {
            return new AcceptInvitationResult(
                AcceptInvitationResultCode.InvitationNotFoundError,
                "Account no longer exists",
                null
            );
        }

        if (accountEntity.Users?.Any(u => u.Id == userId) == true)
        {
            _logger.LogInformation(
                "User {UserId} already in account {AccountId}, invitation burned",
                userId,
                invitationEntity.AccountId
            );
            return new AcceptInvitationResult(
                AcceptInvitationResultCode.Success,
                string.Empty,
                invitationEntity.AccountId
            );
        }

        // Add user to account directly (bypasses AccountProcessor auth)
        var userEntity = await _userRepo.GetAsync(u => u.Id == userId);
        if (userEntity == null)
        {
            return new AcceptInvitationResult(
                AcceptInvitationResultCode.InvitationNotFoundError,
                "User not found",
                null
            );
        }

        accountEntity.Users ??= [];
        accountEntity.Users.Add(userEntity);
        await _accountRepo.UpdateAsync(accountEntity);

        _logger.LogInformation(
            "User {UserId} added to account {AccountId} via invitation {InvitationId}",
            userId,
            invitationEntity.AccountId,
            invitationEntity.Id
        );

        return new AcceptInvitationResult(
            AcceptInvitationResultCode.Success,
            string.Empty,
            invitationEntity.AccountId
        );
    }

    public async Task<RevokeInvitationResult> RevokeInvitationAsync(Guid invitationId)
    {
        var invitationEntity = await _invitationRepo.GetAsync(i => i.Id == invitationId);
        if (invitationEntity == null)
        {
            _logger.LogInformation("Invitation with Id {InvitationId} not found", invitationId);
            return new RevokeInvitationResult(
                RevokeInvitationResultCode.InvitationNotFoundError,
                $"Invitation with Id {invitationId} not found"
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

        _logger.LogInformation("Invitation {InvitationId} revoked", invitationId);
        return new RevokeInvitationResult(RevokeInvitationResultCode.Success, string.Empty);
    }

    public async Task<ListResult<Invitation>> ListInvitationsAsync(
        Guid accountId,
        FilterBuilder<Invitation>? filter,
        OrderBuilder<Invitation>? order,
        int skip,
        int take,
        InvitationStatus? statusFilter = null
    )
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        Expression<Func<InvitationEntity, bool>> scopePredicate = statusFilter switch
        {
            InvitationStatus.Pending => i =>
                i.AccountId == accountId
                && i.AcceptedByUserId == null
                && i.RevokedUtc == null
                && i.ExpiresUtc >= utcNow,
            InvitationStatus.Accepted => i =>
                i.AccountId == accountId && i.AcceptedByUserId != null,
            InvitationStatus.Revoked => i => i.AccountId == accountId && i.RevokedUtc != null,
            InvitationStatus.Expired => i =>
                i.AccountId == accountId
                && i.AcceptedByUserId == null
                && i.RevokedUtc == null
                && i.ExpiresUtc < utcNow,
            _ => i => i.AccountId == accountId,
        };

        return await ListQueryHelper.ExecuteListAsync(
            _invitationRepo,
            scopePredicate,
            filter,
            order,
            skip,
            take,
            e => e.ToModel()
        );
    }
}
