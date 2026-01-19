using Corely.IAM.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Corely.IAM.DataAccessMigrations.MariaDb;

internal class IAMDesignTimeDbContextFactory : IDesignTimeDbContextFactory<IamDbContext>
{
    public IamDbContext CreateDbContext(string[] args)
    {
        var configuration = new EFMariaDbConfiguration(
            MariaDbDesignTimeConstants.DesignTimeConnectionString
        );
        var optionsBuilder = new DbContextOptionsBuilder<IamDbContext>();
        configuration.Configure(optionsBuilder);
        return new IamDbContext(configuration);
    }
}
