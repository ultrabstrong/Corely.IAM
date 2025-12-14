using Corely.IAM.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Corely.IAM.DataAccessMigrations;

internal class IAMDesignTimeDbContextFactory : IDesignTimeDbContextFactory<IamDbContext>
{
    public IamDbContext CreateDbContext(string[] args)
    {
        try
        {
            var configuration = new EFMySqlConfiguration(
                ConfigurationProvider.GetConnectionString()
            );
            var optionsBuilder = new DbContextOptionsBuilder<IamDbContext>();
            configuration.Configure(optionsBuilder);
            return new IamDbContext(configuration);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }
}
