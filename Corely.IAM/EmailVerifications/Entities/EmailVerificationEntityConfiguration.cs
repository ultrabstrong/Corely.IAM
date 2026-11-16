using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.Users.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.EmailVerifications.Entities;

internal sealed class EmailVerificationEntityConfiguration
    : EntityConfigurationBase<EmailVerificationEntity>
{
    public EmailVerificationEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<EmailVerificationEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.Email).IsRequired().HasMaxLength(User.EMAIL_MAX_LENGTH);
        builder
            .Property(e => e.SecretHash)
            .IsRequired()
            .HasMaxLength(EmailVerificationEntity.SECRET_HASH_MAX_LENGTH);

        builder.Property(e => e.SentUtc).IsRequired();
        builder.Property(e => e.ExpiresUtc).IsRequired();
        builder.Property(e => e.CompletedUtc).IsRequired(false);
        builder.Property(e => e.InvalidatedUtc).IsRequired(false);

        builder.HasIndex(e => e.UserId);

        builder
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
