using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Users.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Invitations.Entities;

internal sealed class InvitationEntityConfiguration : EntityConfigurationBase<InvitationEntity>
{
    public InvitationEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<InvitationEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder
            .Property(e => e.SecretHash)
            .IsRequired()
            .HasMaxLength(InvitationEntity.SECRET_HASH_MAX_LENGTH);

        builder.Property(e => e.Email).IsRequired().HasMaxLength(User.EMAIL_MAX_LENGTH);

        builder.Property(e => e.Description).HasMaxLength(Invitation.DESCRIPTION_MAX_LENGTH);

        builder.HasIndex(e => e.AccountId);

        builder.Property(e => e.AcceptedByUserId).IsRequired(false);
        builder.Property(e => e.AcceptedUtc).IsRequired(false);
        builder.Property(e => e.RevokedUtc).IsRequired(false);
    }
}
