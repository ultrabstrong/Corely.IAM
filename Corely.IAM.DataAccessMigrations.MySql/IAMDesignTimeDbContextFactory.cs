using Corely.IAM.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Corely.IAM.DataAccessMigrations.MySql;

internal class IAMDesignTimeDbContextFactory : IDesignTimeDbContextFactory<IamDbContext>
{
    public const string DESIGN_TIME_MARKER = "designtimeonly";

    public const string DESIGN_TIME_CONNECTION_STRING =
        $"Server={DESIGN_TIME_MARKER};Port=1;Database={DESIGN_TIME_MARKER};Uid={DESIGN_TIME_MARKER};Pwd={DESIGN_TIME_MARKER};";

    public IamDbContext CreateDbContext(string[] args)
    {
        var configuration = new EFMySqlConfiguration(DESIGN_TIME_CONNECTION_STRING);
        var optionsBuilder = new DbContextOptionsBuilder<IamDbContext>();
        configuration.Configure(optionsBuilder);
        return new IamDbContext(configuration);
    }
}
