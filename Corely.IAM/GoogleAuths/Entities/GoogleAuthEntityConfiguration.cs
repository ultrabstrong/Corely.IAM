using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.GoogleAuths.Constants;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.GoogleAuths.Entities;

internal sealed class GoogleAuthEntityConfiguration : EntityConfigurationBase<GoogleAuthEntity>
{
    public GoogleAuthEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<GoogleAuthEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.HasIndex(e => e.UserId).IsUnique();

        builder
            .Property(e => e.GoogleSubjectId)
            .IsRequired()
            .HasMaxLength(GoogleAuthConstants.GOOGLE_SUBJECT_ID_MAX_LENGTH);

        builder.HasIndex(e => e.GoogleSubjectId).IsUnique();

        builder
            .Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(GoogleAuthConstants.EMAIL_MAX_LENGTH);
    }
}
