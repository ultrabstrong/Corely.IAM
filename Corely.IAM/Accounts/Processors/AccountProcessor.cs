using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Mappers;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Security.Processors;
using Corely.IAM.Users.Entities;
using Corely.IAM.Validators;
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
}
