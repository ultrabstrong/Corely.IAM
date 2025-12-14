using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.Groups.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Groups.Entities;

internal sealed class GroupEntityConfiguration : EntityConfigurationBase<GroupEntity, int>
{
    public GroupEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<GroupEntity> builder)
    {
        builder
            .Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(GroupConstants.GROUP_NAME_MAX_LENGTH);

        builder.HasIndex(e => new { e.AccountId, e.Name }).IsUnique();

        builder
            .HasMany(e => e.Roles)
            .WithMany(e => e.Groups)
            .UsingEntity(j => j.ToTable("GroupRoles"));
    }
}
