using Corely.IAM.IdentityProviders.Enums;

namespace Corely.IAM.IdentityProviders.Models;

public record IdentityProviderOption(
    IdentityProviderType Type,
    string DisplayName,
    bool AllowsSignUp
);
