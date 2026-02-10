using Corely.IAM.Accounts.Models;
using Corely.IAM.Filtering;
using Corely.IAM.Filtering.Ordering;
using Corely.IAM.Models;

namespace Corely.IAM.Accounts.Processors;

internal interface IAccountProcessor
{
    Task<CreateAccountResult> CreateAccountAsync(CreateAccountRequest request);
    Task<GetAccountResult> GetAccountAsync(Guid accountId);
    Task<AddUserToAccountResult> AddUserToAccountAsync(AddUserToAccountRequest request);
    Task<RemoveUserFromAccountResult> RemoveUserFromAccountAsync(
        RemoveUserFromAccountRequest request
    );
    Task<DeleteAccountResult> DeleteAccountAsync(Guid accountId);
    Task<ListResult<Account>> ListAccountsAsync(
        FilterBuilder<Account>? filter,
        OrderBuilder<Account>? order,
        int skip,
        int take
    );
    Task<GetResult<Account>> GetAccountByIdAsync(Guid accountId, bool hydrate);
}
