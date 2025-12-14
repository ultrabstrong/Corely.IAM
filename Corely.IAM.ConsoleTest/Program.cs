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
                        new ServiceFactory(services, hostContext.Configuration).AddIAMServices();
                    }
                )
                .Build();

            var userContextProvider = host.Services.GetRequiredService<IUserContextProvider>();
            var registrationService = host.Services.GetRequiredService<IRegistrationService>();
            var deregistrationService = host.Services.GetRequiredService<IDeregistrationService>();
            var authenticationService = host.Services.GetRequiredService<IAuthenticationService>();

            var registerUserRequest = new RegisterUserRequest("user1", "email@x.y", "admin");
            var registerUserResult = await registrationService.RegisterUserAsync(
                registerUserRequest
            );

            var registerAccountRequest = new RegisterAccountRequest("acct1");
            var registerAccountResult = await registrationService.RegisterAccountAsync(
                registerAccountRequest
            );

            var registerUser2Request = new RegisterUserRequest("user2", "email2@x.y", "password2");
            var registerUser2Result = await registrationService.RegisterUserAsync(
                registerUser2Request
            );

            // Sign in to get a token and set user context
            var signInForContextRequest = new SignInRequest(
                "user1",
                "admin",
                registerAccountResult.CreatedAccountId
            );
            var signInForContextResult = await authenticationService.SignInAsync(
                signInForContextRequest
            );
            if (signInForContextResult.ResultCode != SignInResultCode.Success)
            {
                throw new Exception(
                    $"Failed to sign in for user context setup: {signInForContextResult.ResultCode}"
                );
            }
            await userContextProvider.SetUserContextAsync(signInForContextResult.AuthToken!);

            var registerUserWithAccountRequest = new RegisterUserWithAccountRequest(
                registerUser2Result.CreatedUserId
            );
            var registerUserWithAccountResult =
                await registrationService.RegisterUserWithAccountAsync(
                    registerUserWithAccountRequest
                );

            var registerGroupRequest = new RegisterGroupRequest("grp1");
            var registerGroupResult = await registrationService.RegisterGroupAsync(
                registerGroupRequest
            );

            var registerPermissionRequest = new RegisterPermissionRequest(
                "group",
                registerGroupResult.CreatedGroupId,
                Read: true
            );
            var registerPermissionResult = await registrationService.RegisterPermissionAsync(
                registerPermissionRequest
            );

            var registerUsersWithGroupRequest = new RegisterUsersWithGroupRequest(
                [registerUserResult.CreatedUserId, 9999, 8888],
                registerGroupResult.CreatedGroupId
            );
            var registerUsersWithGroupResult =
                await registrationService.RegisterUsersWithGroupAsync(
                    registerUsersWithGroupRequest
                );

            var registerRoleRequest = new RegisterRoleRequest("role1");
            var registerRoleResult = await registrationService.RegisterRoleAsync(
                registerRoleRequest
            );

            var registerPermissionsWithRoleRequest = new RegisterPermissionsWithRoleRequest(
                [registerPermissionResult.CreatedPermissionId, 9999, 8888],
                registerRoleResult.CreatedRoleId
            );
            var registerPermissionsWithRoleResult =
                await registrationService.RegisterPermissionsWithRoleAsync(
                    registerPermissionsWithRoleRequest
                );

            // Assign role to user2 for deregistration example
            var registerRolesWithUserRequest = new RegisterRolesWithUserRequest(
                [registerRoleResult.CreatedRoleId],
                registerUser2Result.CreatedUserId
            );
            var registerRolesWithUserResult = await registrationService.RegisterRolesWithUserAsync(
                registerRolesWithUserRequest
            );

            // Assign role to group for deregistration example
            var registerRolesWithGroupRequest = new RegisterRolesWithGroupRequest(
                [registerRoleResult.CreatedRoleId],
                registerGroupResult.CreatedGroupId
            );
            var registerRolesWithGroupResult =
                await registrationService.RegisterRolesWithGroupAsync(
                    registerRolesWithGroupRequest
                );

            // ========= AUTHENTICATION ==========
            var signInRequest = new SignInRequest("user1", "admin");
            var signInResult = await authenticationService.SignInAsync(signInRequest);

            var token = new JwtSecurityTokenHandler().ReadJwtToken(signInResult.AuthToken!);
            var signedOut = await authenticationService.SignOutAsync(
                registerUserResult.CreatedUserId,
                token.Id
            );

            await authenticationService.SignOutAllAsync(registerUserResult.CreatedUserId);

            // ========= DEREGISTERING ==========

            // Deregister roles from group example
            var deregisterRolesFromGroupRequest = new DeregisterRolesFromGroupRequest(
                [registerRoleResult.CreatedRoleId],
                registerGroupResult.CreatedGroupId
            );
            var deregisterRolesFromGroupResult =
                await deregistrationService.DeregisterRolesFromGroupAsync(
                    deregisterRolesFromGroupRequest
                );

            // Deregister roles from user example
            var deregisterRolesFromUserRequest = new DeregisterRolesFromUserRequest(
                [registerRoleResult.CreatedRoleId],
                registerUser2Result.CreatedUserId
            );
            var deregisterRolesFromUserResult =
                await deregistrationService.DeregisterRolesFromUserAsync(
                    deregisterRolesFromUserRequest
                );

            // Deregister permissions from role example
            var deregisterPermissionsFromRoleRequest = new DeregisterPermissionsFromRoleRequest(
                [registerPermissionResult.CreatedPermissionId],
                registerRoleResult.CreatedRoleId
            );
            var deregisterPermissionsFromRoleResult =
                await deregistrationService.DeregisterPermissionsFromRoleAsync(
                    deregisterPermissionsFromRoleRequest
                );

            var deregisterUserRequest = new DeregisterUserRequest(registerUserResult.CreatedUserId);
            var deregisterUserResult = await deregistrationService.DeregisterUserAsync(
                deregisterUserRequest
            );

            var deregisterUserFromAccountRequest = new DeregisterUserFromAccountRequest(
                registerUserResult.CreatedUserId
            );
            var deregisterUserFromAccountResult =
                await deregistrationService.DeregisterUserFromAccountAsync(
                    deregisterUserFromAccountRequest
                );
            deregisterUserFromAccountRequest = new DeregisterUserFromAccountRequest(
                registerUser2Result.CreatedUserId
            );
            deregisterUserFromAccountResult =
                await deregistrationService.DeregisterUserFromAccountAsync(
                    deregisterUserFromAccountRequest
                );
            deregisterUserFromAccountRequest = new DeregisterUserFromAccountRequest(
                registerUser2Result.CreatedUserId
            );
            deregisterUserFromAccountResult =
                await deregistrationService.DeregisterUserFromAccountAsync(
                    deregisterUserFromAccountRequest
                );

            var deregisterPermissionRequest = new DeregisterPermissionRequest(
                registerPermissionResult.CreatedPermissionId
            );
            var deregisterPermissionResult = await deregistrationService.DeregisterPermissionAsync(
                deregisterPermissionRequest
            );

            var deregisterRoleRequest = new DeregisterRoleRequest(registerRoleResult.CreatedRoleId);
            var deregisterRoleResult = await deregistrationService.DeregisterRoleAsync(
                deregisterRoleRequest
            );

            var deregisterGroupRequest = new DeregisterGroupRequest(
                registerGroupResult.CreatedGroupId
            );
            var deregisterGroupResult = await deregistrationService.DeregisterGroupAsync(
                deregisterGroupRequest
            );

            var deregisterAccountResult = await deregistrationService.DeregisterAccountAsync();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "An error occurred");
        }
        Log.CloseAndFlush();
        Log.Logger.Information("Program finished.");
    }
}
