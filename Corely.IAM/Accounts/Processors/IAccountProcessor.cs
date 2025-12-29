using Corely.IAM.Accounts.Models;

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
}
