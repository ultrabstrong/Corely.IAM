using Corely.IAM.BasicAuths.Models;

namespace Corely.IAM.BasicAuths.Processors;

internal interface IBasicAuthProcessor
{
    Task<UpsertBasicAuthResult> UpsertBasicAuthAsync(UpsertBasicAuthRequest request);
    Task<VerifyBasicAuthResult> VerifyBasicAuthAsync(VerifyBasicAuthRequest request);
}
