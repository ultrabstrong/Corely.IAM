using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Mappers;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Security.Processors;
using Corely.IAM.Users.Entities;
using Corely.IAM.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Accounts.Processors;

internal class AccountProcessor : IAccountProcessor
{
    private readonly IRepo<AccountEntity> _accountRepo;
    private readonly IReadonlyRepo<UserEntity> _userRepo;
    private readonly ISecurityProcessor _securityService;
    private readonly IValidationProvider _validationProvider;
    private readonly ILogger<AccountProcessor> _logger;

    public AccountProcessor(
        IRepo<AccountEntity> accountRepo,
        IReadonlyRepo<UserEntity> userRepo,
        ISecurityProcessor securityService,
        IValidationProvider validationProvider,
        ILogger<AccountProcessor> logger
    )
    {
        _accountRepo = accountRepo.ThrowIfNull(nameof(accountRepo));
        _userRepo = userRepo.ThrowIfNull(nameof(userRepo));
        _securityService = securityService.ThrowIfNull(nameof(securityService));
        _validationProvider = validationProvider.ThrowIfNull(nameof(validationProvider));
        _logger = logger.ThrowIfNull(nameof(logger));
    }

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
}
