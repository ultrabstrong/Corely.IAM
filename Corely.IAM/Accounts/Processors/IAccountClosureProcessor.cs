using Corely.IAM.Accounts.Models;

namespace Corely.IAM.Accounts.Processors;

internal interface IAccountClosureProcessor
{
    Task<CloseAccountResult> RemoveAccountAccessAsync(Guid accountId);
    Task<CloseAccountResult> PurgeAccountDataAsync(Guid accountId);
}
