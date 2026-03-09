using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;

namespace Corely.IAM.Accounts.Processors;

internal interface IAccountProcessor
{
    Task<CreateAccountResult> CreateAccountAsync(CreateAccountRequest request);
    Task<GetAccountResult> GetAccountAsync(Guid accountId);
    Task<ModifyResult> UpdateAccountAsync(UpdateAccountRequest request);
    Task<AddUserToAccountResult> AddUserToAccountAsync(AddUserToAccountRequest request);
    Task<AddUserToAccountResult> AddUserToAccountForInvitationAsync(
        AddUserToAccountRequest request
    );
    Task<RemoveUserFromAccountResult> RemoveUserFromAccountAsync(
        RemoveUserFromAccountRequest request
    );
    Task<DeleteAccountResult> DeleteAccountAsync(Guid accountId);
    Task<ListResult<Account>> ListAccountsAsync(ListAccountsRequest request);
    Task<GetResult<Account>> GetAccountByIdAsync(Guid accountId, bool hydrate);
    Task<GetResult<AccountEntity>> GetAccountKeysAsync(Guid accountId);
}
