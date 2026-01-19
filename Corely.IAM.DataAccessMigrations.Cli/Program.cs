using System.CommandLine;
using System.Reflection;
using Corely.IAM.DataAccess;
using Corely.IAM.DataAccessMigrations.Cli.Commands;
using Corely.IAM.DataAccessMigrations.MariaDb;
using Corely.IAM.DataAccessMigrations.MySql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Corely.IAM.DataAccessMigrations.Cli;

internal class Program
{
    private static readonly string CommandsNamespace = typeof(CommandBase).Namespace!;

    static async Task Main(string[] args)
    {
        try
        {
            using var host = new HostBuilder()
                .ConfigureServices(
                    (hostContext, services) =>
                    {
                        services.AddTransient(sp =>
                        {
                            var provider = ConfigurationProvider.GetProvider();
                            var connectionString = ConfigurationProvider.GetConnectionString();
                            var tempServices = new ServiceCollection();

                            switch (provider)
                            {
                                case DatabaseProvider.MySql:
                                    tempServices.AddMySqlIamDbContext(connectionString);
                                    break;
                                case DatabaseProvider.MariaDb:
                                    tempServices.AddMariaDbIamDbContext(connectionString);
                                    break;
                            }

                            var tempProvider = tempServices.BuildServiceProvider();
                            return tempProvider.GetRequiredService<IamDbContext>();
                        });

                        var commandBaseTypes = Assembly
                            .GetExecutingAssembly()
                            .GetTypes()
                            .Where(t => t.IsSubclassOf(typeof(CommandBase)) && !t.IsAbstract);

                        foreach (var type in commandBaseTypes)
                        {
                            services.AddTransient(type);
                        }

                        services.AddTransient<RootCommand>();
                    }
                )
                .Build();

            using var scope = host.Services.CreateScope();

            var rootCommand = GetRootCommand(scope.ServiceProvider);
            rootCommand.Description = "IAM Database Migration Management Tool";

            await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.ResetColor();
        }
    }

    static RootCommand GetRootCommand(IServiceProvider serviceProvider)
    {
        var allCommandTypes = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(type =>
                type.Namespace != null
                && type.Namespace.StartsWith(CommandsNamespace)
                && type.IsSubclassOf(typeof(CommandBase))
                && !type.IsAbstract
            )
            .ToList();

        var topLevelCommands = allCommandTypes
            .Where(type => type.Namespace == CommandsNamespace)
            .Select(type => serviceProvider.GetService(type) as CommandBase)
            .Where(instance => instance != null)
            .ToList();

        var rootCommand = serviceProvider.GetRequiredService<RootCommand>();
        foreach (var command in topLevelCommands)
        {
            if (command != null)
            {
                AddSubCommands(serviceProvider, command, allCommandTypes);
                rootCommand.AddCommand(command);
            }
        }

        return rootCommand;
    }

    static void AddSubCommands(
        IServiceProvider serviceProvider,
        CommandBase parentCommand,
        List<Type> allCommandTypes
    )
    {
        var parentTypeName = parentCommand.GetType().Name;
        var expectedSubNamespace = $"{CommandsNamespace}.{parentTypeName}Commands";

        var subCommandTypes = allCommandTypes
            .Where(type => type.Namespace == expectedSubNamespace)
            .ToList();

        foreach (var subCommandType in subCommandTypes)
        {
            var subCommand = serviceProvider.GetService(subCommandType) as CommandBase;
            if (subCommand != null)
            {
                AddSubCommands(serviceProvider, subCommand, allCommandTypes);
                parentCommand.AddCommand(subCommand);
            }
        }
    }
}
