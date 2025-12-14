using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.Roles.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Roles.Entities;

internal sealed class RoleEntityConfiguration : EntityConfigurationBase<RoleEntity, int>
{
    public RoleEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<RoleEntity> builder)
    {
        builder.Property(e => e.Name).IsRequired().HasMaxLength(RoleConstants.ROLE_NAME_MAX_LENGTH);

        builder.HasIndex(e => new { e.AccountId, e.Name }).IsUnique();

        builder
            .HasMany(e => e.Permissions)
            .WithMany(e => e.Roles)
            .UsingEntity(j => j.ToTable("RolePermissions"));
    }
}
