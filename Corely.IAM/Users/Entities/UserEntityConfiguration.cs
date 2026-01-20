using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.DataAccess.Extensions;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.Entities;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Users.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Users.Entities;

internal sealed class UserEntityConfiguration : EntityConfigurationBase<UserEntity>
{
    public UserEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<UserEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

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
            .UsingEntity<UserAccount>(
                j =>
                    j.HasOne<AccountEntity>()
                        .WithMany()
                        .HasForeignKey(e => e.AccountsId)
                        .OnDelete(DeleteBehavior.NoAction),
                j =>
                    j.HasOne<UserEntity>()
                        .WithMany()
                        .HasForeignKey(e => e.UsersId)
                        .OnDelete(DeleteBehavior.NoAction),
                j => j.ConfigureTable()
            );

        builder
            .HasMany(e => e.Groups)
            .WithMany(e => e.Users)
            .UsingEntity<UserGroup>(
                j =>
                    j.HasOne<GroupEntity>()
                        .WithMany()
                        .HasForeignKey(e => e.GroupsId)
                        .OnDelete(DeleteBehavior.NoAction),
                j =>
                    j.HasOne<UserEntity>()
                        .WithMany()
                        .HasForeignKey(e => e.UsersId)
                        .OnDelete(DeleteBehavior.NoAction),
                j => j.ConfigureTable()
            );

        builder
            .HasMany(e => e.Roles)
            .WithMany(e => e.Users)
            .UsingEntity<UserRole>(
                j =>
                    j.HasOne<RoleEntity>()
                        .WithMany()
                        .HasForeignKey(e => e.RolesId)
                        .OnDelete(DeleteBehavior.NoAction),
                j =>
                    j.HasOne<UserEntity>()
                        .WithMany()
                        .HasForeignKey(e => e.UsersId)
                        .OnDelete(DeleteBehavior.NoAction),
                j => j.ConfigureTable()
            );

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
