using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.Users.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Users.Entities;

internal sealed class UserEntityConfiguration : EntityConfigurationBase<UserEntity, int>
{
    public UserEntityConfiguration(IEFDbTypes efDbTypes)
        : base(efDbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<UserEntity> builder)
    {
        builder.Property(e => e.PublicId).IsRequired();

        builder.HasIndex(e => e.PublicId).IsUnique();

        builder.Property(e => e.Disabled).IsRequired();

        builder
            .Property(e => e.Username)
            .HasMaxLength(UserConstants.USERNAME_MAX_LENGTH)
            .IsRequired();

        builder.HasIndex(e => e.Username).IsUnique();

        builder.Property(e => e.Email).HasMaxLength(UserConstants.EMAIL_MAX_LENGTH).IsRequired();

        builder.HasIndex(e => e.Email).IsUnique();

        builder
            .HasOne(p => p.BasicAuth)
            .WithOne(d => d.User)
            .HasForeignKey<BasicAuthEntity>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(e => e.Accounts)
            .WithMany(e => e.Users)
            .UsingEntity(j => j.ToTable("UserAccounts"));

        builder
            .HasMany(e => e.Groups)
            .WithMany(e => e.Users)
            .UsingEntity(j => j.ToTable("UserGroups"));

        builder
            .HasMany(e => e.Roles)
            .WithMany(e => e.Users)
            .UsingEntity(j => j.ToTable("UserRoles"));

        builder
            .HasMany(e => e.SymmetricKeys)
            .WithOne()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(e => e.AsymmetricKeys)
            .WithOne()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
