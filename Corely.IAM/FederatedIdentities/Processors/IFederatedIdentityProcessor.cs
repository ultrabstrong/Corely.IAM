using Corely.IAM.FederatedIdentities.Models;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;

namespace Corely.IAM.FederatedIdentities.Processors;

internal interface IFederatedIdentityProcessor
{
    Task<Guid?> FindUserIdAsync(string issuer, string subject);
    Task<LinkFederatedIdentityResult> LinkAsync(Guid userId, ExternalIdentity identity);
    Task<LinkFederatedIdentityResult> LinkToNewUserAsync(Guid userId, ExternalIdentity identity);
    Task<UnlinkFederatedIdentityResult> UnlinkAsync(Guid userId, IdentityProviderType type);
    Task<IReadOnlyList<LinkedIdentity>> ListAsync(Guid userId);
    Task<bool> RecordReauthenticationAsync(Guid userId, ExternalIdentity identity, Guid sessionId);
    Task<bool> ConsumeReauthenticationAsync(Guid userId, Guid sessionId);
    Task<bool> HasAnyAsync(Guid userId);
    Task<int> CountSoleSignInUsersAsync(IdentityProviderType type);
}
