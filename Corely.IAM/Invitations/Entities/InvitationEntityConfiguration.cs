using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.Invitations.Constants;
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
            .Property(e => e.Token)
            .IsRequired()
            .HasMaxLength(InvitationConstants.TOKEN_MAX_LENGTH);

        builder.HasIndex(e => e.Token).IsUnique();

        builder
            .Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(InvitationConstants.EMAIL_MAX_LENGTH);

        builder
            .Property(e => e.Description)
            .HasMaxLength(InvitationConstants.DESCRIPTION_MAX_LENGTH);

        builder.HasIndex(e => e.AccountId);

        builder.Property(e => e.AcceptedByUserId).IsRequired(false);
        builder.Property(e => e.AcceptedUtc).IsRequired(false);
        builder.Property(e => e.RevokedUtc).IsRequired(false);
    }
}
