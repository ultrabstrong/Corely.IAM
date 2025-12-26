using Corely.IAM.BasicAuths.Models;

namespace Corely.IAM.BasicAuths.Processors;

internal interface IBasicAuthProcessor
{
    Task<CreateBasicAuthResult> CreateBasicAuthAsync(CreateBasicAuthRequest request);
    Task<UpdateBasicAuthResult> UpdateBasicAuthAsync(UpdateBasicAuthRequest request);
    Task<VerifyBasicAuthResult> VerifyBasicAuthAsync(VerifyBasicAuthRequest request);
}
