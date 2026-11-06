using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using MsSqlModel = Corely.IAM.CompiledModel.MsSql.Generated.IamDbContextModel;
using MySqlModel = Corely.IAM.CompiledModel.MySql.Generated.IamDbContextModel;

namespace Corely.IAM.UnitTests.DataAccess;

public class CompiledModelTests
{
    private sealed class MsSqlConfiguration()
        : EFMsSqlConfigurationBase("Server=compiled-model-test")
    {
        public override void Configure(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseSqlServer(connectionString);
    }

    private sealed class MySqlConfiguration()
        : EFMySqlConfigurationBase("Server=compiled-model-test;Database=iam")
    {
        public override void Configure(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseMySQL(connectionString);
    }

    private static IEnumerable<string> Shape(IRelationalModel model) =>
        model
            .Tables.OrderBy(t => t.Schema)
            .ThenBy(t => t.Name)
            .SelectMany(t =>
                new[] { $"table {t.Schema}.{t.Name}" }
                    .Concat(
                        t.Columns.OrderBy(c => c.Name)
                            .Select(c =>
                                $"column {t.Name}.{c.Name} {c.StoreType} nullable={c.IsNullable}"
                            )
                    )
                    .Concat(
                        t.PrimaryKey is { } key
                            ? [$"key {t.Name} {string.Join(",", key.Columns.Select(c => c.Name))}"]
                            : []
                    )
                    .Concat(
                        t.ForeignKeyConstraints.OrderBy(f => f.Name)
                            .Select(f =>
                                $"foreign key {t.Name}.{f.Name} ({string.Join(",", f.Columns.Select(c => c.Name))}) -> {f.PrincipalTable.Name} {f.OnDeleteAction}"
                            )
                    )
                    .Concat(
                        t.Indexes.OrderBy(i => i.Name)
                            .Select(i =>
                                $"index {t.Name}.{i.Name} ({string.Join(",", i.Columns.Select(c => c.Name))}) unique={i.IsUnique}"
                            )
                    )
            );

    private static void AssertMatches(IEFConfiguration configuration, IModel compiledModel)
    {
        using var builtContext = new IamDbContext(configuration);
        using var compiledContext = new IamDbContext(
            new DbContextOptions<IamDbContext>(),
            configuration,
            new IamCompiledModel(compiledModel)
        );

        Assert.Same(compiledModel, compiledContext.Model);
        Assert.Equal(
            Shape(builtContext.GetService<IDesignTimeModel>().Model.GetRelationalModel()),
            Shape(compiledContext.Model.GetRelationalModel())
        );
    }

    [Fact]
    public void MsSqlModel_DescribesTheSameDatabaseAsTheBuiltModel_ForSqlServer() =>
        AssertMatches(new MsSqlConfiguration(), MsSqlModel.Instance);

    [Fact]
    public void MySqlModel_DescribesTheSameDatabaseAsTheBuiltModel_ForMySql() =>
        AssertMatches(new MySqlConfiguration(), MySqlModel.Instance);
}
