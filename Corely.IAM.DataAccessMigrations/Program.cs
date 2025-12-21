using System.CommandLine;
using System.Reflection;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.DataAccess;
using Corely.IAM.DataAccessMigrations.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Corely.IAM.DataAccessMigrations;

internal class Program
{
    private const string CommandsNamespace = "Corely.IAM.DataAccessMigrations.Commands";

    static async Task Main(string[] args)
    {
        try
        {
            using var host = new HostBuilder()
                .ConfigureServices(
                    (hostContext, services) =>
                    {
                        // Register DbContext factory - only creates context when actually needed
                        // This allows --help and config commands to work without a settings file
                        services.AddTransient(sp =>
                        {
                            var connectionString = ConfigurationProvider.GetConnectionString();
                            var configuration = new EFMySqlConfiguration(connectionString);
                            var optionsBuilder =
                                new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<IamDbContext>();
                            configuration.Configure(optionsBuilder);
                            return new IamDbContext(optionsBuilder.Options, configuration);
                        });

                        // Register EF configuration for injection with deferred loading
                        services.AddTransient<IEFConfiguration>(sp => new EFMySqlConfiguration(
                            ConfigurationProvider.GetConnectionString()
                        ));

                        // Register all command types
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
        // Get all command types from the assembly
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

        // Top-level commands are directly in the Commands namespace
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
        // Get the expected namespace for subcommands of this parent
        // e.g., if parent is "Database" in "Commands" namespace,
        // subcommands are in "Commands.DatabaseCommands" namespace
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
