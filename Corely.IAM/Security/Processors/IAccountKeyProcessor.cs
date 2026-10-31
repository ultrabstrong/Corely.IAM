using Corely.IAM.Models;
using Corely.IAM.Security.Models;

namespace Corely.IAM.Security.Processors;

internal interface IAccountKeyProcessor
{
    Task<GetResult<AccountJsonWebKeySet>> GetJsonWebKeySetAsync(Guid accountId);
    Task<RevealAccountKeyResult> RevealKeyAsync(RevealAccountKeyRequest request);
}
