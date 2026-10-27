using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.DataAccess.Extensions;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Entities;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Roles.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.ApiKeys.Entities;

internal sealed class ApiKeyEntityConfiguration : EntityConfigurationBase<ApiKeyEntity>
{
    public ApiKeyEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<ApiKeyEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Name).IsRequired().HasMaxLength(ApiKey.NAME_MAX_LENGTH);
        builder.Property(e => e.Description).HasMaxLength(ApiKey.DESCRIPTION_MAX_LENGTH);
        builder.Property(e => e.LockedUtc).IsRequired(false);

        builder.HasIndex(e => new { e.AccountId, e.Name }).IsUnique();

        builder
            .HasOne(e => e.Account)
            .WithMany(a => a.ApiKeys)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(e => e.Secrets)
            .WithOne(s => s.ApiKey)
            .HasForeignKey(s => s.ApiKeyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(e => e.Groups)
            .WithMany(g => g.ApiKeys)
            .UsingEntity<ApiKeyGroup>(
                j =>
                    j.HasOne<GroupEntity>()
                        .WithMany()
                        .HasForeignKey(e => e.GroupsId)
                        .OnDelete(DeleteBehavior.NoAction),
                j =>
                    j.HasOne<ApiKeyEntity>()
                        .WithMany()
                        .HasForeignKey(e => e.ApiKeysId)
                        .OnDelete(DeleteBehavior.NoAction),
                j => j.ConfigureTable()
            );

        builder
            .HasMany(e => e.Roles)
            .WithMany(r => r.ApiKeys)
            .UsingEntity<ApiKeyRole>(
                j =>
                    j.HasOne<RoleEntity>()
                        .WithMany()
                        .HasForeignKey(e => e.RolesId)
                        .OnDelete(DeleteBehavior.NoAction),
                j =>
                    j.HasOne<ApiKeyEntity>()
                        .WithMany()
                        .HasForeignKey(e => e.ApiKeysId)
                        .OnDelete(DeleteBehavior.NoAction),
                j => j.ConfigureTable()
            );
    }
}
