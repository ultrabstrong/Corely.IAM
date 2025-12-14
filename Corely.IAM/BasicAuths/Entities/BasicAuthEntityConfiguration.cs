using Corely.DataAccess;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.BasicAuths.Constants;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Corely.IAM.BasicAuths.Entities;

internal sealed class BasicAuthEntityConfiguration : EntityConfigurationBase<BasicAuthEntity, int>
{
    public BasicAuthEntityConfiguration(IDbTypes dbTypes)
        : base(dbTypes) { }

    protected override void ConfigureInternal(EntityTypeBuilder<BasicAuthEntity> builder)
    {
        builder
            .Property(x => x.Password)
            .IsRequired()
            .HasMaxLength(BasicAuthConstants.PASSWORD_MAX_LENGTH);
    }
}
