using Corely.IAM.Accounts.Models;

namespace Corely.IAM.Accounts.Processors;

internal interface IAccountProcessor
{
    Task<CreateAccountResult> CreateAccountAsync(CreateAccountRequest request);
    Task<GetAccountResult> GetAccountAsync(int accountId);
    Task<GetAccountResult> GetAccountAsync(string accountName);
    Task<ListAccountsForUserResult> ListAccountsForUserAsync(int userId);
    Task<AddUserToAccountResult> AddUserToAccountAsync(AddUserToAccountRequest request);
    Task<RemoveUserFromAccountResult> RemoveUserFromAccountAsync(
        RemoveUserFromAccountRequest request
    );
    Task<DeleteAccountResult> DeleteAccountAsync(int accountId);
}
