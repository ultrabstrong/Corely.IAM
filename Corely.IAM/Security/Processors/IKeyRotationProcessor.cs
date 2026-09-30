using Corely.IAM.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;

namespace Corely.IAM.Security.Processors;

internal interface IKeyRotationProcessor
{
    Task<ModifyResult> RotateAccountKeyAsync(RotateAccountKeyRequest request);
    Task<ModifyResult> RotateCurrentUserKeyAsync(KeyType keyType);
}
