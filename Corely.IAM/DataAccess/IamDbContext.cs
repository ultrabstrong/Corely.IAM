using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.DataAccess;

internal class IamDbContext(IEFConfiguration efConfiguration) : DbContext
{
    private readonly IEFConfiguration _efConfiguration = efConfiguration;

    public IamDbContext(DbContextOptions<IamDbContext> opts, IEFConfiguration efConfiguration)
        : this(efConfiguration)
    {
        // Options are passed to the base class via OnConfiguring
    }

    public DbSet<AccountEntity> Accounts { get; set; } = null!;
    public DbSet<UserEntity> Users { get; set; } = null!;
    public DbSet<BasicAuthEntity> BasicAuths { get; set; } = null!;
    public DbSet<UserAuthTokenEntity> UserAuthTokens { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        _efConfiguration.Configure(optionsBuilder);
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
