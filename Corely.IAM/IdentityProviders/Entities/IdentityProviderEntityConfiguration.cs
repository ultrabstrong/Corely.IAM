using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.IdentityProviders.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.IdentityProviders.Entities;

internal sealed class IdentityProviderEntityConfiguration
    : EntityConfigurationBase<IdentityProviderEntity>
{
    public IdentityProviderEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<IdentityProviderEntity> builder)
    {
        builder.HasKey(e => e.Type);
        builder.Property(e => e.Type).ValueGeneratedNever();

        builder
            .Property(e => e.ClientId)
            .IsRequired()
            .HasMaxLength(IdentityProviderSettings.CLIENT_ID_MAX_LENGTH);

        builder.Property(e => e.EncryptedCredential).IsRequired(false);

        builder
            .Property(e => e.CertificateThumbprint)
            .HasMaxLength(IdentityProviderSettings.CERTIFICATE_THUMBPRINT_MAX_LENGTH);

        builder.Property(e => e.Tenant).HasMaxLength(IdentityProviderSettings.TENANT_MAX_LENGTH);
    }
}
