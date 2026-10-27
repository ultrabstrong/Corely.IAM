using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.ApiKeys.Entities;

internal sealed class ApiKeySecretEntityConfiguration : EntityConfigurationBase<ApiKeySecretEntity>
{
    public ApiKeySecretEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<ApiKeySecretEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Prefix).IsRequired().HasMaxLength(ApiKeySecretEntity.PREFIX_LENGTH);
        builder.HasIndex(e => e.Prefix).IsUnique();

        builder
            .Property(e => e.SecretHash)
            .IsRequired()
            .HasMaxLength(ApiKeySecretEntity.SECRET_HASH_MAX_LENGTH);

        builder.HasIndex(e => new { e.ApiKeyId, e.Slot }).IsUnique();
    }
}
