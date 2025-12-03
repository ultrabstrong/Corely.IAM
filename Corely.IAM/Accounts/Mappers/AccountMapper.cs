using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;

namespace Corely.IAM.Accounts.Mappers;

internal static class AccountMapper
{
    public static Account ToAccount(this CreateAccountRequest request)
    {
        return new Account { AccountName = request.AccountName };
    }

    public static AccountEntity ToEntity(this Account account)
    {
        return new AccountEntity { Id = account.Id, AccountName = account.AccountName };
    }

    public static Account ToModel(this AccountEntity entity)
    {
        return new Account { Id = entity.Id, AccountName = entity.AccountName };
    }
}
