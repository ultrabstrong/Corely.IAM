namespace Corely.IAM.IdentityProviders.Models;

internal sealed record StoredIdentityProvider(
    IdentityProviderSettings Settings,
    string? EncryptedCredential,
    string? PlainCredential
);
