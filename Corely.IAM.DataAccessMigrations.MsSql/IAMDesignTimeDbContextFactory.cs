using Corely.IAM.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Corely.IAM.DataAccessMigrations.MsSql;

internal class IAMDesignTimeDbContextFactory : IDesignTimeDbContextFactory<IamDbContext>
{
    public const string DESIGN_TIME_MARKER = "designtimeonly";

    public const string DESIGN_TIME_CONNECTION_STRING =
        $"Server={DESIGN_TIME_MARKER};Database={DESIGN_TIME_MARKER};Trusted_Connection=True;TrustServerCertificate=True;";

    public IamDbContext CreateDbContext(string[] args)
    {
        var configuration = new EFMsSqlConfiguration(DESIGN_TIME_CONNECTION_STRING);
        var optionsBuilder = new DbContextOptionsBuilder<IamDbContext>();
        configuration.Configure(optionsBuilder);
        return new IamDbContext(configuration);
    }
}
