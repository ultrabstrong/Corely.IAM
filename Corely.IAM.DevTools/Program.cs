using System.CommandLine;
using System.Reflection;
using Corely.Common.Providers.Redaction;
using Corely.IAM.DevTools.Commands;
using Corely.IAM.DevTools.SerilogCustomization;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
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
            .Enrich.WithProperty("CorrelationId", Guid.NewGuid())
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
                        var exePath = AppContext.BaseDirectory;
                        config.SetBasePath(exePath);
                        config.AddJsonFile(
                            "appsettings.json",
                            optional: false,
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

            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
            var userContextProvider =
                scope.ServiceProvider.GetRequiredService<IUserContextProvider>();

            var username = configuration.GetValue<string>("DevToolsUserContext:Username");
            var password = configuration.GetValue<string>("DevToolsUserContext:Password");
            var accountPublicIdString = configuration.GetValue<string>(
                "DevToolsUserContext:AccountPublicId"
            );
            Guid? accountPublicId = null;
            if (
                !string.IsNullOrEmpty(accountPublicIdString)
                && Guid.TryParse(accountPublicIdString, out var parsedGuid)
            )
            {
                accountPublicId = parsedGuid;
            }

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var signInResult = await authService.SignInAsync(
                    new SignInRequest(username, password, accountPublicId)
                );
                if (
                    signInResult.ResultCode == SignInResultCode.Success
                    && signInResult.AuthToken != null
                )
                {
                    await userContextProvider.SetUserContextAsync(signInResult.AuthToken);
                }
            }

            var rootCommand = GetRootCommand(scope.ServiceProvider);
            await rootCommand.InvokeAsync(args);
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
        var commandInstances = AppDomain
            .CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type =>
                !type.IsNested
                && type.Namespace != null
                && type.Namespace.StartsWith(typeof(CommandBase).Namespace!)
                && type.IsSubclassOf(typeof(CommandBase))
            )
            .Select(type => serviceProvider.GetService(type) as CommandBase)
            .Where(instance => instance != null)
            .ToList();

        var rootCommand = serviceProvider.GetRequiredService<RootCommand>();
        foreach (var command in commandInstances)
        {
            if (command != null)
            {
                AddSubCommands(serviceProvider, command);
                rootCommand.AddCommand(command);
            }
        }

        return rootCommand;
    }

    static void AddSubCommands(IServiceProvider serviceProvider, CommandBase command)
    {
        var subCommandInstances = command
            .GetType()
            .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .Where(type => type.IsSubclassOf(typeof(CommandBase)))
            .Select(type => serviceProvider.GetService(type) as CommandBase)
            .Where(instance => instance != null)
            .ToList();

        foreach (var subCommand in subCommandInstances)
        {
            AddSubCommands(serviceProvider, subCommand!);
            command.AddCommand(subCommand!);
        }
    }
}
