using Corely.IAM.IdentityProviders.Enums;

namespace Corely.IAM.FederatedIdentities.Models;

public record LinkedIdentity(IdentityProviderType Type, string? Email, DateTime LinkedUtc);
