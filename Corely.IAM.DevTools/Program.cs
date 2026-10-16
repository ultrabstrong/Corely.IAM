using System.CommandLine;
using System.Reflection;
using Corely.Common.Providers.Redaction;
using Corely.IAM.DevTools.Commands;
using Corely.IAM.DevTools.SerilogCustomization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Corely.IAM.DevTools;

internal class Program
{
    static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Fatal)
            .MinimumLevel.Override("System", LogEventLevel.Fatal)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Corely.IAM.DevTools")
            .Enrich.WithProperty("CorrelationId", Guid.CreateVersion7())
            .Enrich.With(new SerilogRedactionEnricher([new PasswordRedactionProvider()]))
            .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
            .WriteTo.Seq("http://localhost:5341")
            .CreateLogger();

        try
        {
            using var host = new HostBuilder()
                .ConfigureAppConfiguration(
                    (hostingContext, config) =>
                    {
                        config.SetBasePath(AppContext.BaseDirectory);
                        config.AddJsonFile(
                            ConfigurationProvider.SETTINGS_FILE_NAME,
                            optional: true,
                            reloadOnChange: true
                        );
                    }
                )
                .ConfigureServices(
                    (hostContext, services) =>
                    {
                        ServiceFactory.RegisterServices(services, hostContext.Configuration);

                        var commandBaseTypes = AppDomain
                            .CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
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
            await rootCommand.Parse(args).InvokeAsync();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "An error occurred");
        }
        Log.CloseAndFlush();
        Log.Logger.Information("Program finished.");
    }

    static RootCommand GetRootCommand(IServiceProvider serviceProvider)
    {
        var skipped = new List<(Type Type, Exception Error)>();
        var commandInstances = AppDomain
            .CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type =>
                !type.IsNested
                && type.Namespace != null
                && type.Namespace.StartsWith(typeof(CommandBase).Namespace!)
                && type.IsSubclassOf(typeof(CommandBase))
            )
            .Select(type => TryGetService(serviceProvider, type, skipped) as CommandBase)
            .Where(instance => instance != null)
            .ToList();

        var rootCommand = serviceProvider.GetRequiredService<RootCommand>();
        foreach (var command in commandInstances)
        {
            if (command != null)
            {
                AddSubCommands(serviceProvider, command, skipped);
                rootCommand.Subcommands.Add(command);
            }
        }

        ReportSkipped(serviceProvider, skipped);
        return rootCommand;
    }

    static void AddSubCommands(
        IServiceProvider serviceProvider,
        CommandBase command,
        List<(Type Type, Exception Error)> skipped
    )
    {
        var subCommandInstances = command
            .GetType()
            .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .Where(type => type.IsSubclassOf(typeof(CommandBase)))
            .Select(type => TryGetService(serviceProvider, type, skipped) as CommandBase)
            .Where(instance => instance != null)
            .ToList();

        foreach (var subCommand in subCommandInstances)
        {
            AddSubCommands(serviceProvider, subCommand!, skipped);
            command.Subcommands.Add(subCommand!);
        }
    }

    static object? TryGetService(
        IServiceProvider serviceProvider,
        Type type,
        List<(Type Type, Exception Error)> skipped
    )
    {
        try
        {
            return serviceProvider.GetService(type);
        }
        catch (Exception ex)
        {
            skipped.Add((type, ex));
            return null;
        }
    }

    static void ReportSkipped(
        IServiceProvider serviceProvider,
        List<(Type Type, Exception Error)> skipped
    )
    {
        if (skipped.Count == 0)
            return;

        var groups = skipped.Select(s => (s.Type.DeclaringType ?? s.Type).Name).Distinct().Order();
        var reason =
            serviceProvider.GetService<IamServicesUnavailable>()?.Reason
            ?? skipped[0].Error.GetBaseException().Message;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(
            $"{skipped.Count} commands are unavailable ({string.Join(", ", groups)}): {reason}"
        );
        Console.ResetColor();
    }
}
