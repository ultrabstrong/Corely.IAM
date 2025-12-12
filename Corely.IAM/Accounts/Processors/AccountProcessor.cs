using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Mappers;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Security.Processors;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Processors;
using Corely.IAM.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Accounts.Processors;

internal class AccountProcessor(
    IRepo<AccountEntity> accountRepo,
    IReadonlyRepo<UserEntity> userRepo,
    IUserOwnershipProcessor userOwnershipProcessor,
    ISecurityProcessor securityService,
    IValidationProvider validationProvider,
    ILogger<AccountProcessor> logger
) : IAccountProcessor
{
    private readonly IRepo<AccountEntity> _accountRepo = accountRepo.ThrowIfNull(
        nameof(accountRepo)
    );
    private readonly IReadonlyRepo<UserEntity> _userRepo = userRepo.ThrowIfNull(nameof(userRepo));
    private readonly IUserOwnershipProcessor _userOwnershipProcessor =
        userOwnershipProcessor.ThrowIfNull(nameof(userOwnershipProcessor));
    private readonly ISecurityProcessor _securityService = securityService.ThrowIfNull(
        nameof(securityService)
    );
    private readonly IValidationProvider _validationProvider = validationProvider.ThrowIfNull(
        nameof(validationProvider)
    );
    private readonly ILogger<AccountProcessor> _logger = logger.ThrowIfNull(nameof(logger));

    public async Task<CreateAccountResult> CreateAccountAsync(CreateAccountRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var account = request.ToAccount();
        _validationProvider.ThrowIfInvalid(account);

        var existingAccount = await _accountRepo.GetAsync(a =>
            a.AccountName == request.AccountName
        );
        if (existingAccount != null)
        {
            _logger.LogWarning("Account {Account} already exists", request.AccountName);
            return new CreateAccountResult(
                CreateAccountResultCode.AccountExistsError,
                $"Account {request.AccountName} already exists",
                -1
            );
        }

        var userEntity = await _userRepo.GetAsync(u => u.Id == request.OwnerUserId);
        if (userEntity == null)
        {
            _logger.LogWarning("User with Id {UserId} not found", request.OwnerUserId);
            return new CreateAccountResult(
                CreateAccountResultCode.UserOwnerNotFoundError,
                $"User with Id {request.OwnerUserId} not found",
                -1
            );
        }

        account.SymmetricKeys =
        [
            _securityService.GetSymmetricEncryptionKeyEncryptedWithSystemKey(),
        ];
        account.AsymmetricKeys =
        [
            _securityService.GetAsymmetricEncryptionKeyEncryptedWithSystemKey(),
            _securityService.GetAsymmetricSignatureKeyEncryptedWithSystemKey(),
        ];

        var accountEntity = account.ToEntity();
        accountEntity.Users = [userEntity];
        var created = await _accountRepo.CreateAsync(accountEntity);

        return new CreateAccountResult(CreateAccountResultCode.Success, string.Empty, created.Id);
    }

    public async Task<Account?> GetAccountAsync(int accountId)
    {
        var accountEntity = await _accountRepo.GetAsync(a => a.Id == accountId);
        var account = accountEntity?.ToModel();

        if (account == null)
        {
            _logger.LogInformation("Account with Id {AccountId} not found", accountId);
        }
        return account;
    }

    public async Task<Account?> GetAccountAsync(string accountName)
    {
        var accountEntity = await _accountRepo.GetAsync(a => a.AccountName == accountName);
        var account = accountEntity?.ToModel();

        if (account == null)
        {
            _logger.LogInformation("Account with name {AccountName} not found", accountName);
        }

        return account;
    }

    public async Task<List<Account>> GetAccountsForUserAsync(int userId)
    {
        var accountEntities = await _accountRepo.ListAsync(a =>
            a.Users != null && a.Users.Any(u => u.Id == userId)
        );
        return accountEntities.Select(a => a.ToModel()).ToList();
    }

    public async Task<DeleteAccountResult> DeleteAccountAsync(int accountId)
    {
        var accountEntity = await _accountRepo.GetAsync(a => a.Id == accountId);
        if (accountEntity == null)
        {
            _logger.LogWarning("Account with Id {AccountId} not found", accountId);
            return new DeleteAccountResult(
                DeleteAccountResultCode.AccountNotFoundError,
                $"Account with Id {accountId} not found"
            );
        }

        await _accountRepo.DeleteAsync(accountEntity);

        _logger.LogInformation("Account with Id {AccountId} deleted", accountId);
        return new DeleteAccountResult(DeleteAccountResultCode.Success, string.Empty);
    }

    public async Task<AddUserToAccountResult> AddUserToAccountAsync(AddUserToAccountRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var userEntity = await _userRepo.GetAsync(u => u.Id == request.UserId);
        if (userEntity == null)
        {
            _logger.LogWarning("User with Id {UserId} not found", request.UserId);
            return new AddUserToAccountResult(
                AddUserToAccountResultCode.UserNotFoundError,
                $"User with Id {request.UserId} not found"
            );
        }

        var accountEntity = await _accountRepo.GetAsync(
            a => a.Id == request.AccountId,
            include: q => q.Include(a => a.Users)
        );
        if (accountEntity == null)
        {
            _logger.LogWarning("Account with Id {AccountId} not found", request.AccountId);
            return new AddUserToAccountResult(
                AddUserToAccountResultCode.AccountNotFoundError,
                $"Account with Id {request.AccountId} not found"
            );
        }

        if (accountEntity.Users?.Any(u => u.Id == request.UserId) == true)
        {
            _logger.LogWarning(
                "User with Id {UserId} is already in account {AccountId}",
                request.UserId,
                request.AccountId
            );
            return new AddUserToAccountResult(
                AddUserToAccountResultCode.UserAlreadyInAccountError,
                $"User with Id {request.UserId} is already in account {request.AccountId}"
            );
        }

        accountEntity.Users ??= [];
        accountEntity.Users.Add(userEntity);
        await _accountRepo.UpdateAsync(accountEntity);

        _logger.LogInformation(
            "User with Id {UserId} added to account {AccountId}",
            request.UserId,
            request.AccountId
        );
        return new AddUserToAccountResult(AddUserToAccountResultCode.Success, string.Empty);
    }

    public async Task<RemoveUserFromAccountResult> RemoveUserFromAccountAsync(
        RemoveUserFromAccountRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var userEntity = await _userRepo.GetAsync(u => u.Id == request.UserId);
        if (userEntity == null)
        {
            _logger.LogWarning("User with Id {UserId} not found", request.UserId);
            return new RemoveUserFromAccountResult(
                RemoveUserFromAccountResultCode.UserNotFoundError,
                $"User with Id {request.UserId} not found"
            );
        }

        var accountEntity = await _accountRepo.GetAsync(
            a => a.Id == request.AccountId,
            include: q => q.Include(a => a.Users)
        );
        if (accountEntity == null)
        {
            _logger.LogWarning("Account with Id {AccountId} not found", request.AccountId);
            return new RemoveUserFromAccountResult(
                RemoveUserFromAccountResultCode.AccountNotFoundError,
                $"Account with Id {request.AccountId} not found"
            );
        }

        if (accountEntity.Users?.Any(u => u.Id == request.UserId) != true)
        {
            _logger.LogWarning(
                "User with Id {UserId} is not in account {AccountId}",
                request.UserId,
                request.AccountId
            );
            return new RemoveUserFromAccountResult(
                RemoveUserFromAccountResultCode.UserNotInAccountError,
                $"User with Id {request.UserId} is not in account {request.AccountId}"
            );
        }

        var soleOwnerResult = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(
            request.UserId,
            request.AccountId
        );
        if (soleOwnerResult.IsSoleOwner)
        {
            _logger.LogWarning(
                "User with Id {UserId} is the sole owner of account {AccountId} and cannot be removed",
                request.UserId,
                request.AccountId
            );
            return new RemoveUserFromAccountResult(
                RemoveUserFromAccountResultCode.UserIsSoleOwnerError,
                $"User is the sole owner of account '{accountEntity.AccountName}' (Id: {request.AccountId}) and cannot be removed"
            );
        }

        var userToRemove = accountEntity.Users!.First(u => u.Id == request.UserId);
        accountEntity.Users!.Remove(userToRemove);
        await _accountRepo.UpdateAsync(accountEntity);

        _logger.LogInformation(
            "User with Id {UserId} removed from account {AccountId}",
            request.UserId,
            request.AccountId
        );
        return new RemoveUserFromAccountResult(
            RemoveUserFromAccountResultCode.Success,
            string.Empty
        );
    }
}
