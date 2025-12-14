using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Users.Entities;

internal sealed class UserAuthTokenEntityConfiguration
    : EntityConfigurationBase<UserAuthTokenEntity, int>
{
    public UserAuthTokenEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<UserAuthTokenEntity> builder)
    {
        builder.Property(e => e.UserId).IsRequired();

        builder.Property(e => e.PublicId).HasMaxLength(36).IsRequired(); // GUID length

        builder.HasIndex(e => e.PublicId).IsUnique();

        builder.Property(e => e.IssuedUtc).IsRequired();

        builder.Property(e => e.ExpiresUtc).IsRequired();

        builder.HasIndex(e => e.ExpiresUtc); // For cleanup queries

        builder
            .HasOne(e => e.User)
            .WithMany(u => u.AuthTokens)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
