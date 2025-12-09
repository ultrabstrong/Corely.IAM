using Corely.IAM.Accounts.Models;

namespace Corely.IAM.Accounts.Processors;

internal interface IAccountProcessor
{
    Task<CreateAccountResult> CreateAccountAsync(CreateAccountRequest request);
    Task<Account?> GetAccountAsync(int accountId);
    Task<Account?> GetAccountAsync(string accountName);
    Task<AddUserToAccountResult> AddUserToAccountAsync(AddUserToAccountRequest request);
    Task<DeleteAccountResult> DeleteAccountAsync(int accountId);
}
