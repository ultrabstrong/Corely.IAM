using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Permissions.Entities;

internal sealed class PermissionEntityConfiguration : EntityConfigurationBase<PermissionEntity>
{
    public PermissionEntityConfiguration(IDbTypes eFDbTypes)
        : base(eFDbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<PermissionEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder
            .HasIndex(e => new
            {
                e.AccountId,
                e.ResourceType,
                e.ResourceId,
                e.Create,
                e.Read,
                e.Update,
                e.Delete,
                e.Execute,
            })
            .IsUnique();
    }
}
