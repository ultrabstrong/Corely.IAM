using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Mappers;
using Corely.IAM.Processors;
using Corely.IAM.Security.Processors;
using Corely.IAM.Users.Entities;
using Corely.IAM.Validators;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Accounts.Processors;

internal class AccountProcessor : ProcessorBase, IAccountProcessor
{
    private readonly IRepo<AccountEntity> _accountRepo;
    private readonly IReadonlyRepo<UserEntity> _userRepo;
    private readonly ISecurityProcessor _securityService;

    public AccountProcessor(
        IRepo<AccountEntity> accountRepo,
        IReadonlyRepo<UserEntity> userRepo,
        ISecurityProcessor securityService,
        IMapProvider mapProvider,
        IValidationProvider validationProvider,
        ILogger<AccountProcessor> logger
    )
        : base(mapProvider, validationProvider, logger)
    {
        _accountRepo = accountRepo.ThrowIfNull(nameof(accountRepo));
        _userRepo = userRepo.ThrowIfNull(nameof(userRepo));
        _securityService = securityService.ThrowIfNull(nameof(securityService));
    }

    public async Task<CreateAccountResult> CreateAccountAsync(CreateAccountRequest request)
    {
        return await LogRequestResultAspect(
            nameof(AccountProcessor),
            nameof(CreateAccountAsync),
            request,
            async () =>
            {
                var account = MapThenValidateTo<Account>(request);

                var existingAccount = await _accountRepo.GetAsync(a =>
                    a.AccountName == request.AccountName
                );
                if (existingAccount != null)
                {
                    Logger.LogWarning("Account {Account} already exists", request.AccountName);
                    return new CreateAccountResult(
                        CreateAccountResultCode.AccountExistsError,
                        $"Account {request.AccountName} already exists",
                        -1
                    );
                }

                var userEntity = await _userRepo.GetAsync(u => u.Id == request.OwnerUserId);
                if (userEntity == null)
                {
                    Logger.LogWarning("User with Id {UserId} not found", request.OwnerUserId);
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

                var accountEntity = MapTo<AccountEntity>(account)!; // account is validated
                accountEntity.Users = [userEntity];
                var created = await _accountRepo.CreateAsync(accountEntity);

                return new CreateAccountResult(
                    CreateAccountResultCode.Success,
                    string.Empty,
                    created.Id
                );
            }
        );
    }

    public async Task<Account?> GetAccountAsync(int accountId)
    {
        return await LogRequestAspect(
            nameof(AccountProcessor),
            nameof(GetAccountAsync),
            accountId,
            async () =>
            {
                var accountEntity = await _accountRepo.GetAsync(a => a.Id == accountId);
                var account = MapTo<Account>(accountEntity);

                if (account == null)
                {
                    Logger.LogInformation("Account with Id {AccountId} not found", accountId);
                }
                return account;
            }
        );
    }

    public async Task<Account?> GetAccountAsync(string accountName)
    {
        return await LogRequestResultAspect(
            nameof(AccountProcessor),
            nameof(GetAccountAsync),
            accountName,
            async () =>
            {
                var accountEntity = await _accountRepo.GetAsync(a => a.AccountName == accountName);
                var account = MapTo<Account>(accountEntity);

                if (account == null)
                {
                    Logger.LogInformation("Account with name {AccountName} not found", accountName);
                }

                return account;
            }
        );
    }
}
