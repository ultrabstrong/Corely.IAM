using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.DataAccess.Extensions;
using Corely.IAM.Entities;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Roles.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Roles.Entities;

internal sealed class RoleEntityConfiguration : EntityConfigurationBase<RoleEntity>
{
    public RoleEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<RoleEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Name).IsRequired().HasMaxLength(RoleConstants.ROLE_NAME_MAX_LENGTH);

        builder.HasIndex(e => new { e.AccountId, e.Name }).IsUnique();

        builder
            .HasMany(e => e.Permissions)
            .WithMany(e => e.Roles)
            .UsingEntity<RolePermission>(
                j =>
                    j.HasOne<PermissionEntity>()
                        .WithMany()
                        .HasForeignKey(e => e.PermissionsId)
                        .OnDelete(DeleteBehavior.NoAction),
                j =>
                    j.HasOne<RoleEntity>()
                        .WithMany()
                        .HasForeignKey(e => e.RolesId)
                        .OnDelete(DeleteBehavior.NoAction),
                j => j.ConfigureTable()
            );
    }
}
