using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.Audits.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.Audits.Entities;

internal sealed class AuditEntryEntityConfiguration : EntityConfigurationBase<AuditEntryEntity>
{
    public AuditEntryEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<AuditEntryEntity> builder)
    {
        builder.ToTable(AuditConstants.AUDIT_ENTRIES_TABLE_NAME);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.OccurredUtc).IsRequired();
        builder.Property(e => e.ActorUserId).IsRequired(false);
        builder.Property(e => e.AccountId).IsRequired(false);
        builder.Property(e => e.Cohort).IsRequired();
        builder.Property(e => e.Action).IsRequired();

        builder.Property(e => e.Source).IsRequired().HasMaxLength(AuditConstants.SOURCE_MAX_LENGTH);

        builder
            .Property(e => e.Service)
            .IsRequired()
            .HasMaxLength(AuditConstants.SERVICE_MAX_LENGTH);

        builder
            .Property(e => e.Operation)
            .IsRequired()
            .HasMaxLength(AuditConstants.OPERATION_MAX_LENGTH);

        builder
            .Property(e => e.ResourceType)
            .IsRequired()
            .HasMaxLength(AuditConstants.RESOURCE_TYPE_MAX_LENGTH);

        builder.Property(e => e.ResourceIds).IsRequired(false);

        builder
            .Property(e => e.ResultCode)
            .IsRequired()
            .HasMaxLength(AuditConstants.RESULT_CODE_MAX_LENGTH);

        builder.Property(e => e.Details).HasMaxLength(AuditConstants.DETAILS_MAX_LENGTH);

        builder.HasIndex(e => new { e.AccountId, e.OccurredUtc });
        builder.HasIndex(e => new { e.ActorUserId, e.OccurredUtc });
    }
}
