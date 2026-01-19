using Corely.IAM.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Corely.IAM.DataAccessMigrations.MsSql;

internal class IAMDesignTimeDbContextFactory : IDesignTimeDbContextFactory<IamDbContext>
{
    public IamDbContext CreateDbContext(string[] args)
    {
        var configuration = new EFMsSqlConfiguration(
            MsSqlDesignTimeConstants.DesignTimeConnectionString
        );
        var optionsBuilder = new DbContextOptionsBuilder<IamDbContext>();
        configuration.Configure(optionsBuilder);
        return new IamDbContext(configuration);
    }
}
