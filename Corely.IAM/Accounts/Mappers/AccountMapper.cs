using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Security.Mappers;

namespace Corely.IAM.Accounts.Mappers;

internal static class AccountMapper
{
    extension(CreateAccountRequest request)
    {
        public Account ToAccount()
        {
            return new Account { AccountName = request.AccountName };
        }
    }

    extension(Account account)
    {
        public AccountEntity ToEntity()
        {
            return new AccountEntity
            {
                Id = account.Id,
                AccountName = account.AccountName,
                SymmetricKeys = account
                    .SymmetricKeys?.Select(k => k.ToAccountEntity(account.Id))
                    .ToList(),
                AsymmetricKeys = account
                    .AsymmetricKeys?.Select(k => k.ToAccountEntity(account.Id))
                    .ToList(),
            };
        }
    }

    extension(AccountEntity entity)
    {
        public Account ToModel()
        {
            return new Account { Id = entity.Id, AccountName = entity.AccountName };
        }
    }
}
