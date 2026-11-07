using Corely.IAM.IdentityProviders.Entities;
using Corely.IAM.IdentityProviders.Models;

namespace Corely.IAM.IdentityProviders.Mappers;

internal static class IdentityProviderMapper
{
    extension(IdentityProviderEntity entity)
    {
        public IdentityProviderSettings ToModel() =>
            new(
                entity.Type,
                entity.Enabled,
                entity.AllowSignUp,
                entity.ClientId,
                entity.CredentialKind,
                !string.IsNullOrWhiteSpace(entity.EncryptedCredential),
                entity.CertificateThumbprint,
                entity.Tenant
            );

        public StoredIdentityProvider ToStored() =>
            new(entity.ToModel(), entity.EncryptedCredential, null);

        public void Apply(UpdateIdentityProviderRequest request)
        {
            entity.Enabled = request.Enabled;
            entity.AllowSignUp = request.AllowSignUp;
            entity.ClientId = request.ClientId.Trim();
            entity.Tenant = string.IsNullOrWhiteSpace(request.Tenant)
                ? null
                : request.Tenant.Trim();
            if (entity.CredentialKind != request.CredentialKind && !request.ReplacesCredential)
            {
                entity.EncryptedCredential = null;
                entity.CertificateThumbprint = null;
            }
            entity.CredentialKind = request.CredentialKind;
        }
    }

    extension(StoredIdentityProvider stored)
    {
        public IdentityProviderEntity ToEntity(Func<string, string> encrypt) =>
            new()
            {
                Type = stored.Settings.Type,
                Enabled = stored.Settings.Enabled,
                AllowSignUp = stored.Settings.AllowSignUp,
                ClientId = stored.Settings.ClientId,
                CredentialKind = stored.Settings.CredentialKind,
                EncryptedCredential =
                    stored.EncryptedCredential
                    ?? (
                        string.IsNullOrWhiteSpace(stored.PlainCredential)
                            ? null
                            : encrypt(stored.PlainCredential)
                    ),
                CertificateThumbprint = stored.Settings.CertificateThumbprint,
                Tenant = stored.Settings.Tenant,
            };
    }
}
