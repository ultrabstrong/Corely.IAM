using System.IdentityModel.Tokens.Jwt;
using Corely.Common.Providers.Redaction;
using Corely.IAM.ConsoleApp.SerilogCustomization;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Corely.IAM.ConsoleApp;

internal class Program
{
#pragma warning disable IDE0052 // Remove unread private members
    private static readonly string desktop = Environment.GetFolderPath(
        Environment.SpecialFolder.Desktop
    );
    private static readonly string downloads =
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
#pragma warning restore IDE0052 // Remove unread private members

    static async Task Main()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Fatal)
            .MinimumLevel.Override("System", LogEventLevel.Fatal)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", $"Corely.IAM.{nameof(ConsoleApp)}")
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
                        config.SetBasePath(Directory.GetCurrentDirectory());
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
                    }
                )
                .Build();

            var userContextProvider = host.Services.GetRequiredService<IUserContextProvider>();
            var registrationService = host.Services.GetRequiredService<IRegistrationService>();
            var deregistrationService = host.Services.GetRequiredService<IDeregistrationService>();
            var authenticationService = host.Services.GetRequiredService<IAuthenticationService>();

            var registerUserResult = await registrationService.RegisterUserAsync(
                new RegisterUserRequest("user1", "email@x.y", "admin")
            );

            var registerAccountResult = await registrationService.RegisterAccountAsync(
                new RegisterAccountRequest("acct1")
            );

            var registerUser2Result = await registrationService.RegisterUserAsync(
                new RegisterUserRequest("user2", "email2@x.y", "password2")
            );

            // Sign in without account to get token
            var signInResult = await authenticationService.SignInAsync(
                new SignInRequest("user1", "admin")
            );
            if (signInResult.ResultCode != SignInResultCode.Success)
            {
                throw new Exception($"Failed to sign in: {signInResult.ResultCode}");
            }

            // Switch to the specific account using the token
            var switchAccountResult = await authenticationService.SwitchAccountAsync(
                new SwitchAccountRequest(
                    signInResult.AuthToken!,
                    registerAccountResult.CreatedAccountId
                )
            );
            if (switchAccountResult.ResultCode != SignInResultCode.Success)
            {
                throw new Exception(
                    $"Failed to switch to account: {switchAccountResult.ResultCode}"
                );
            }

            // simulate owner adding other user to account
            var registerUserWithAccountResult =
                await registrationService.RegisterUserWithAccountAsync(
                    new RegisterUserWithAccountRequest(registerUser2Result.CreatedUserId)
                );

            var registerGroupResult = await registrationService.RegisterGroupAsync(
                new RegisterGroupRequest("grp1")
            );

            var registerPermissionResult = await registrationService.RegisterPermissionAsync(
                new RegisterPermissionRequest(
                    "group",
                    registerGroupResult.CreatedGroupId,
                    Read: true
                )
            );

            var registerUsersWithGroupResult =
                await registrationService.RegisterUsersWithGroupAsync(
                    new RegisterUsersWithGroupRequest(
                        [registerUserResult.CreatedUserId, 9999, 8888],
                        registerGroupResult.CreatedGroupId
                    )
                );

            var registerRoleResult = await registrationService.RegisterRoleAsync(
                new RegisterRoleRequest("role1")
            );

            var registerPermissionsWithRoleResult =
                await registrationService.RegisterPermissionsWithRoleAsync(
                    new RegisterPermissionsWithRoleRequest(
                        [registerPermissionResult.CreatedPermissionId, 9999, 8888],
                        registerRoleResult.CreatedRoleId
                    )
                );

            // Assign role to user2 for deregistration example
            var registerRolesWithUserResult = await registrationService.RegisterRolesWithUserAsync(
                new RegisterRolesWithUserRequest(
                    [registerRoleResult.CreatedRoleId],
                    registerUser2Result.CreatedUserId
                )
            );

            // Assign role to group for deregistration example
            var registerRolesWithGroupResult =
                await registrationService.RegisterRolesWithGroupAsync(
                    new RegisterRolesWithGroupRequest(
                        [registerRoleResult.CreatedRoleId],
                        registerGroupResult.CreatedGroupId
                    )
                );

            // ========= AUTHENTICATION ==========
            // Sign in without account first
            signInResult = await authenticationService.SignInAsync(
                new SignInRequest("user1", "admin")
            );

            // Then switch to the specific account
            switchAccountResult = await authenticationService.SwitchAccountAsync(
                new SwitchAccountRequest(
                    signInResult.AuthToken!,
                    registerAccountResult.CreatedAccountId
                )
            );

            // SwitchAccountAsync sets context, but later when all you have is the auth token use this
            await userContextProvider.SetUserContextAsync(switchAccountResult.AuthToken!);

            var token = new JwtSecurityTokenHandler().ReadJwtToken(switchAccountResult.AuthToken!);

            // Uncomment to see all deregister fail
            /*
            var signedOut = await authenticationService.SignOutAsync(
                registerUserResult.CreatedUserId,
                token.Id
            );
            await authenticationService.SignOutAllAsync(registerUserResult.CreatedUserId);
            */

            // ========= DEREGISTERING ==========

            // Deregister roles from group example
            var deregisterRolesFromGroupResult =
                await deregistrationService.DeregisterRolesFromGroupAsync(
                    new DeregisterRolesFromGroupRequest(
                        [registerRoleResult.CreatedRoleId],
                        registerGroupResult.CreatedGroupId
                    )
                );

            // Deregister roles from user example
            var deregisterRolesFromUserResult =
                await deregistrationService.DeregisterRolesFromUserAsync(
                    new DeregisterRolesFromUserRequest(
                        [registerRoleResult.CreatedRoleId],
                        registerUser2Result.CreatedUserId
                    )
                );

            // Deregister permissions from role example
            var deregisterPermissionsFromRoleResult =
                await deregistrationService.DeregisterPermissionsFromRoleAsync(
                    new DeregisterPermissionsFromRoleRequest(
                        [registerPermissionResult.CreatedPermissionId],
                        registerRoleResult.CreatedRoleId
                    )
                );

            // deregister user when user is sole owner fails
            var deregisterUserResult = await deregistrationService.DeregisterUserAsync();

            // Deregister user1 from account fails because user1 is sole owner
            var deregisterUserFromAccountResult =
                await deregistrationService.DeregisterUserFromAccountAsync(
                    new DeregisterUserFromAccountRequest(registerUserResult.CreatedUserId)
                );

            // deregister user2 from account succeeds because user2 is not owner
            deregisterUserFromAccountResult =
                await deregistrationService.DeregisterUserFromAccountAsync(
                    new DeregisterUserFromAccountRequest(registerUser2Result.CreatedUserId)
                );

            var deregisterPermissionResult = await deregistrationService.DeregisterPermissionAsync(
                new DeregisterPermissionRequest(registerPermissionResult.CreatedPermissionId)
            );

            var deregisterRoleResult = await deregistrationService.DeregisterRoleAsync(
                new DeregisterRoleRequest(registerRoleResult.CreatedRoleId)
            );

            var deregisterGroupResult = await deregistrationService.DeregisterGroupAsync(
                new DeregisterGroupRequest(registerGroupResult.CreatedGroupId)
            );

            var deregisterAccountResult = await deregistrationService.DeregisterAccountAsync();

            // deregister user when user is not sole owner succeeds
            deregisterUserResult = await deregistrationService.DeregisterUserAsync();

            // sign in as user 2 and deregister user 2
            signInResult = await authenticationService.SignInAsync(
                new SignInRequest("user2", "password2")
            );
            await deregistrationService.DeregisterUserAsync();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "An error occurred");
        }
        Log.CloseAndFlush();
        Log.Logger.Information("Program finished.");
    }
}
