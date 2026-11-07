using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.ApiKeys.Entities;
using Corely.IAM.Audits.Entities;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.FederatedIdentities.Entities;
using Corely.IAM.IdentityProviders.Entities;
using Corely.IAM.Invitations.Entities;
using Corely.IAM.MfaChallenges.Entities;
using Corely.IAM.PasswordRecoveries.Entities;
using Corely.IAM.Platform.Entities;
using Corely.IAM.Security.Entities;
using Corely.IAM.TotpAuths.Entities;
using Corely.IAM.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.DataAccess;

internal class IamDbContext : DbContext
{
    public const string DEFAULT_MIGRATIONS_HISTORY_TABLE = "__CorelyIamMigrationsHistory";

    private readonly IEFConfiguration _efConfiguration;
    private readonly IModel? _compiledModel;

    public IamDbContext(
        [FromKeyedServices(EFConfigurationKeys.IAM)] IEFConfiguration efConfiguration
    )
    {
        _efConfiguration = efConfiguration;
    }

#pragma warning disable IDE0060
    public IamDbContext(
        DbContextOptions<IamDbContext> opts,
        [FromKeyedServices(EFConfigurationKeys.IAM)] IEFConfiguration efConfiguration,
        [FromKeyedServices(EFConfigurationKeys.IAM)] IamCompiledModel compiledModel
    )
#pragma warning restore IDE0060
    {
        _efConfiguration = efConfiguration;
        _compiledModel = compiledModel.Model;
    }

    public DbSet<AccountEntity> Accounts { get; set; } = null!;
    public DbSet<UserEntity> Users { get; set; } = null!;
    public DbSet<BasicAuthEntity> BasicAuths { get; set; } = null!;
    public DbSet<AuthTokenEntity> AuthTokens { get; set; } = null!;
    public DbSet<InvitationEntity> Invitations { get; set; } = null!;
    public DbSet<PasswordRecoveryEntity> PasswordRecoveries { get; set; } = null!;
    public DbSet<TotpAuthEntity> TotpAuths { get; set; } = null!;
    public DbSet<TotpRecoveryCodeEntity> TotpRecoveryCodes { get; set; } = null!;
    public DbSet<FederatedIdentityEntity> FederatedIdentities { get; set; } = null!;
    public DbSet<IdentityProviderEntity> IdentityProviders { get; set; } = null!;
    public DbSet<MfaChallengeEntity> MfaChallenges { get; set; } = null!;
    public DbSet<AuditEntryEntity> AuditEntries { get; set; } = null!;
    public DbSet<AccountAuditSettingsEntity> AccountAuditSettings { get; set; } = null!;
    public DbSet<PlatformSettingsEntity> PlatformSettings { get; set; } = null!;
    public DbSet<PlatformSecuritySettingsEntity> PlatformSecuritySettings { get; set; } = null!;
    public DbSet<ApiKeyEntity> ApiKeys { get; set; } = null!;
    public DbSet<ApiKeySecretEntity> ApiKeySecrets { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        _efConfiguration.Configure(optionsBuilder);
        if (_compiledModel is not null)
        {
            optionsBuilder.UseModel(_compiledModel);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var configTypes = new Type[]
        {
            typeof(EntityConfigurationBase<>),
            typeof(EntityConfigurationBase<,>),
        };
        foreach (var configType in configTypes)
        {
            var configs = GetType()
                .Assembly.GetTypes()
                .Where(t =>
                    t.IsClass
                    && !t.IsAbstract
                    && t.BaseType?.IsGenericType == true
                    && t.BaseType.GetGenericTypeDefinition() == configType
                );

            foreach (var t in configs)
            {
                var cfg = Activator.CreateInstance(t, _efConfiguration.GetDbTypes());
                modelBuilder.ApplyConfiguration((dynamic)cfg!);
            }
        }
    }
}
