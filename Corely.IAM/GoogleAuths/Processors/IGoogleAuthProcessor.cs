using Corely.IAM.GoogleAuths.Models;

namespace Corely.IAM.GoogleAuths.Processors;

internal interface IGoogleAuthProcessor
{
    Task<LinkGoogleAuthResult> LinkGoogleAuthAsync(Guid userId, string googleIdToken);
    Task<UnlinkGoogleAuthResult> UnlinkGoogleAuthAsync(Guid userId);
    Task<AuthMethodsResult> GetAuthMethodsAsync(Guid userId);
    Task<Guid?> GetUserIdByGoogleSubjectAsync(string googleSubjectId);
}
