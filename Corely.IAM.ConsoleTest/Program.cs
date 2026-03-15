using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Corely.Common.Providers.Redaction;
using Corely.IAM.Accounts.Models;
using Corely.IAM.ConsoleApp.SerilogCustomization;
using Corely.IAM.Groups.Models;
using Corely.IAM.Invitations.Constants;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.TotpAuths.Providers;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Corely.IAM.ConsoleApp;

internal class Program
{
    private const string TEST_DEVICE_ID = "console-test-device";

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
            var retrievalService = host.Services.GetRequiredService<IRetrievalService>();
            var modificationService = host.Services.GetRequiredService<IModificationService>();
            var mfaService = host.Services.GetRequiredService<IMfaService>();
            var googleAuthService = host.Services.GetRequiredService<IGoogleAuthService>();
            var invitationService = host.Services.GetRequiredService<IInvitationService>();

            // ========= REGISTER USER 1 ==========
            var registerUserResult = await registrationService.RegisterUserAsync(
                new RegisterUserRequest("user1", "email@x.y", "admin")
            );

            // Sign in as user1 to establish context before creating account
            var signInResult = await authenticationService.SignInAsync(
                new SignInRequest("user1", "admin", TEST_DEVICE_ID)
            );
            if (signInResult.ResultCode != SignInResultCode.Success)
            {
                throw new Exception($"Failed to sign in: {signInResult.ResultCode}");
            }

            // ========= REGISTER ACCOUNT ==========
            var registerAccountResult = await registrationService.RegisterAccountAsync(
                new RegisterAccountRequest("acct1")
            );

            // Switch to the account after creating it (context is already set from sign in)
            var switchAccountResult = await authenticationService.SwitchAccountAsync(
                new SwitchAccountRequest(registerAccountResult.CreatedAccountId)
            );
            if (switchAccountResult.ResultCode != SignInResultCode.Success)
            {
                throw new Exception(
                    $"Failed to switch to account: {switchAccountResult.ResultCode}"
                );
            }

            // ========= REGISTER USER 2 ==========
            var registerUser2Result = await registrationService.RegisterUserAsync(
                new RegisterUserRequest("user2", "email2@x.y", "password2")
            );

            // Add user2 to account (owner action)
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
                        [
                            registerUserResult.CreatedUserId,
                            Guid.CreateVersion7(),
                            Guid.CreateVersion7(),
                        ],
                        registerGroupResult.CreatedGroupId
                    )
                );

            var registerRoleResult = await registrationService.RegisterRoleAsync(
                new RegisterRoleRequest("role1")
            );

            var registerPermissionsWithRoleResult =
                await registrationService.RegisterPermissionsWithRoleAsync(
                    new RegisterPermissionsWithRoleRequest(
                        [
                            registerPermissionResult.CreatedPermissionId,
                            Guid.CreateVersion7(),
                            Guid.CreateVersion7(),
                        ],
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

            // ========= RE-AUTHENTICATE ==========
            // Sign in again to demonstrate auth flow
            signInResult = await authenticationService.SignInAsync(
                new SignInRequest("user1", "admin", TEST_DEVICE_ID)
            );

            // Switch to account (context is set from sign in above)
            switchAccountResult = await authenticationService.SwitchAccountAsync(
                new SwitchAccountRequest(registerAccountResult.CreatedAccountId)
            );

            var token = new JwtSecurityTokenHandler().ReadJwtToken(switchAccountResult.AuthToken!);

            // Uncomment to see all deregister fail
            /*
             var signOutRequest = new SignOutRequest(token.Id);
               var signedOut = await authenticationService.SignOutAsync(signOutRequest);
       await authenticationService.SignOutAllAsync();
 */

            // ========= RETRIEVAL SERVICE ========== //

            // List all groups
            var listGroupsResult = await retrievalService.ListGroupsAsync(new ListGroupsRequest());
            Console.WriteLine($"List Groups: {JsonSerializer.Serialize(listGroupsResult)}");

            // List all roles
            var listRolesResult = await retrievalService.ListRolesAsync(new ListRolesRequest());
            Console.WriteLine($"List Roles: {JsonSerializer.Serialize(listRolesResult)}");

            // List all permissions
            var listPermissionsResult = await retrievalService.ListPermissionsAsync(
                new ListPermissionsRequest()
            );
            Console.WriteLine(
                $"List Permissions: {JsonSerializer.Serialize(listPermissionsResult)}"
            );

            // List all users
            var listUsersResult = await retrievalService.ListUsersAsync(new ListUsersRequest());
            Console.WriteLine($"List Users: {JsonSerializer.Serialize(listUsersResult)}");

            // List all accounts
            var listAccountsResult = await retrievalService.ListAccountsAsync(
                new ListAccountsRequest()
            );
            Console.WriteLine($"List Accounts: {JsonSerializer.Serialize(listAccountsResult)}");

            // Get group by ID with hydration
            var getGroupResult = await retrievalService.GetGroupAsync(
                registerGroupResult.CreatedGroupId,
                hydrate: true
            );
            Console.WriteLine($"Get Group (hydrated): {JsonSerializer.Serialize(getGroupResult)}");

            // Get role by ID with hydration
            var getRoleResult = await retrievalService.GetRoleAsync(
                registerRoleResult.CreatedRoleId,
                hydrate: true
            );
            Console.WriteLine($"Get Role (hydrated): {JsonSerializer.Serialize(getRoleResult)}");

            // Get user by ID with hydration
            var getUserResult = await retrievalService.GetUserAsync(
                registerUserResult.CreatedUserId,
                hydrate: true
            );
            Console.WriteLine($"Get User (hydrated): {JsonSerializer.Serialize(getUserResult)}");

            // Get account by ID with hydration
            var getAccountResult = await retrievalService.GetAccountAsync(
                registerAccountResult.CreatedAccountId,
                hydrate: true
            );
            Console.WriteLine(
                $"Get Account (hydrated): {JsonSerializer.Serialize(getAccountResult)}"
            );

            // ========= ACCOUNT KEY PROVIDERS ========== //

            // Get account symmetric encryption provider and encrypt/decrypt
            var symProviderResult =
                await retrievalService.GetAccountSymmetricEncryptionProviderAsync(
                    registerAccountResult.CreatedAccountId
                );
            Console.WriteLine(
                $"Get Symmetric Encryption Provider: ResultCode={symProviderResult.ResultCode}"
            );
            if (symProviderResult.Item != null)
            {
                var plaintext = "Hello from ConsoleTest!";
                var encrypted = symProviderResult.Item.Encrypt(plaintext);
                var decrypted = symProviderResult.Item.Decrypt(encrypted);
                Console.WriteLine($"  Symmetric Encrypt: {plaintext} -> {encrypted}");
                Console.WriteLine($"  Symmetric Decrypt: {encrypted} -> {decrypted}");
                Console.WriteLine($"  Round-trip match: {plaintext == decrypted}");
            }

            // Get account asymmetric encryption provider and encrypt/decrypt
            var asymEncProviderResult =
                await retrievalService.GetAccountAsymmetricEncryptionProviderAsync(
                    registerAccountResult.CreatedAccountId
                );
            Console.WriteLine(
                $"Get Asymmetric Encryption Provider: ResultCode={asymEncProviderResult.ResultCode}"
            );
            if (asymEncProviderResult.Item != null)
            {
                var plaintext = "Asymmetric encryption test";
                var encrypted = asymEncProviderResult.Item.Encrypt(plaintext);
                var decrypted = asymEncProviderResult.Item.Decrypt(encrypted);
                Console.WriteLine($"  Asymmetric Encrypt: {plaintext} -> {encrypted}");
                Console.WriteLine($"  Asymmetric Decrypt: {encrypted} -> {decrypted}");
                Console.WriteLine($"  Round-trip match: {plaintext == decrypted}");
            }

            // Get account asymmetric signature provider and sign/verify
            var sigProviderResult =
                await retrievalService.GetAccountAsymmetricSignatureProviderAsync(
                    registerAccountResult.CreatedAccountId
                );
            Console.WriteLine(
                $"Get Asymmetric Signature Provider: ResultCode={sigProviderResult.ResultCode}"
            );
            if (sigProviderResult.Item != null)
            {
                var payload = "Sign this payload";
                var signature = sigProviderResult.Item.Sign(payload);
                var isValid = sigProviderResult.Item.Verify(payload, signature);
                var isTampered = sigProviderResult.Item.Verify("tampered", signature);
                Console.WriteLine($"  Sign: {payload} -> {signature}");
                Console.WriteLine($"  Verify original: {isValid}");
                Console.WriteLine($"  Verify tampered: {isTampered}");
            }

            // ========= USER KEY PROVIDERS ========== //

            // Get user symmetric encryption provider and encrypt/decrypt
            var userSymProviderResult =
                await retrievalService.GetUserSymmetricEncryptionProviderAsync();
            Console.WriteLine(
                $"Get User Symmetric Encryption Provider: ResultCode={userSymProviderResult.ResultCode}"
            );
            if (userSymProviderResult.Item != null)
            {
                var plaintext = "Hello from user key!";
                var encrypted = userSymProviderResult.Item.Encrypt(plaintext);
                var decrypted = userSymProviderResult.Item.Decrypt(encrypted);
                Console.WriteLine($"  User Symmetric Encrypt: {plaintext} -> {encrypted}");
                Console.WriteLine($"  User Symmetric Decrypt: {encrypted} -> {decrypted}");
                Console.WriteLine($"  Round-trip match: {plaintext == decrypted}");
            }

            // Get user asymmetric encryption provider and encrypt/decrypt
            var userAsymEncProviderResult =
                await retrievalService.GetUserAsymmetricEncryptionProviderAsync();
            Console.WriteLine(
                $"Get User Asymmetric Encryption Provider: ResultCode={userAsymEncProviderResult.ResultCode}"
            );
            if (userAsymEncProviderResult.Item != null)
            {
                var plaintext = "User asymmetric encryption test";
                var encrypted = userAsymEncProviderResult.Item.Encrypt(plaintext);
                var decrypted = userAsymEncProviderResult.Item.Decrypt(encrypted);
                Console.WriteLine($"  User Asymmetric Encrypt: {plaintext} -> {encrypted}");
                Console.WriteLine($"  User Asymmetric Decrypt: {encrypted} -> {decrypted}");
                Console.WriteLine($"  Round-trip match: {plaintext == decrypted}");
            }

            // Get user asymmetric signature provider and sign/verify
            var userSigProviderResult =
                await retrievalService.GetUserAsymmetricSignatureProviderAsync();
            Console.WriteLine(
                $"Get User Asymmetric Signature Provider: ResultCode={userSigProviderResult.ResultCode}"
            );
            if (userSigProviderResult.Item != null)
            {
                var payload = "Sign this user payload";
                var signature = userSigProviderResult.Item.Sign(payload);
                var isValid = userSigProviderResult.Item.Verify(payload, signature);
                var isTampered = userSigProviderResult.Item.Verify("tampered", signature);
                Console.WriteLine($"  Sign: {payload} -> {signature}");
                Console.WriteLine($"  Verify original: {isValid}");
                Console.WriteLine($"  Verify tampered: {isTampered}");
            }

            // ========= MODIFICATION SERVICE ========== //

            // Update account name
            var modifyAccountResult = await modificationService.ModifyAccountAsync(
                new UpdateAccountRequest(registerAccountResult.CreatedAccountId, "acct1-updated")
            );
            Console.WriteLine($"Modify Account: {JsonSerializer.Serialize(modifyAccountResult)}");

            // Update user1 username and email
            var modifyUserResult = await modificationService.ModifyUserAsync(
                new UpdateUserRequest(
                    registerUserResult.CreatedUserId,
                    "user1-updated",
                    "updated-email@x.y"
                )
            );
            Console.WriteLine($"Modify User: {JsonSerializer.Serialize(modifyUserResult)}");

            // Update group name and description
            var modifyGroupResult = await modificationService.ModifyGroupAsync(
                new UpdateGroupRequest(
                    registerGroupResult.CreatedGroupId,
                    "grp1-updated",
                    "Updated group description"
                )
            );
            Console.WriteLine($"Modify Group: {JsonSerializer.Serialize(modifyGroupResult)}");

            // Update role name and description
            var modifyRoleResult = await modificationService.ModifyRoleAsync(
                new UpdateRoleRequest(
                    registerRoleResult.CreatedRoleId,
                    "role1-updated",
                    "Updated role description"
                )
            );
            Console.WriteLine($"Modify Role: {JsonSerializer.Serialize(modifyRoleResult)}");

            // ========= INVITATION LIFECYCLE ==========

            // Create an invitation for the current account
            var createInvitationResult = await invitationService.CreateInvitationAsync(
                new CreateInvitationRequest(
                    registerAccountResult.CreatedAccountId,
                    "test@example.com",
                    "Test invitation",
                    InvitationConstants.DEFAULT_EXPIRY_SECONDS
                )
            );
            Console.WriteLine(
                $"Create Invitation: {JsonSerializer.Serialize(createInvitationResult)}"
            );
            Console.WriteLine(
                $"  Token: {createInvitationResult.Token}, InvitationId: {createInvitationResult.InvitationId}"
            );

            // Accept the invitation (idempotent — user is already in account from account creation)
            var acceptInvitationResult = await invitationService.AcceptInvitationAsync(
                new AcceptInvitationRequest(createInvitationResult.Token!)
            );
            Console.WriteLine(
                $"Accept Invitation: {JsonSerializer.Serialize(acceptInvitationResult)}"
            );

            // List invitations for the account (shows accepted status)
            var listInvitationsResult = await invitationService.ListInvitationsAsync(
                new ListInvitationsRequest(registerAccountResult.CreatedAccountId)
            );
            Console.WriteLine(
                $"List Invitations (after accept): {JsonSerializer.Serialize(listInvitationsResult)}"
            );

            // Create a second invitation that will be revoked
            var createInvitation2Result = await invitationService.CreateInvitationAsync(
                new CreateInvitationRequest(
                    registerAccountResult.CreatedAccountId,
                    "test2@example.com",
                    "Will be revoked",
                    InvitationConstants.DEFAULT_EXPIRY_SECONDS
                )
            );
            Console.WriteLine(
                $"Create Invitation 2: {JsonSerializer.Serialize(createInvitation2Result)}"
            );

            // Revoke the second invitation
            var revokeInvitationResult = await invitationService.RevokeInvitationAsync(
                createInvitation2Result.InvitationId!.Value
            );
            Console.WriteLine(
                $"Revoke Invitation: {JsonSerializer.Serialize(revokeInvitationResult)}"
            );

            // List invitations again (shows one accepted, one revoked)
            listInvitationsResult = await invitationService.ListInvitationsAsync(
                new ListInvitationsRequest(registerAccountResult.CreatedAccountId)
            );
            Console.WriteLine(
                $"List Invitations (final): {JsonSerializer.Serialize(listInvitationsResult)}"
            );

            // ========= MFA (TOTP) DEMO ==========

            // Enable TOTP for user1
            var enableTotpResult = await mfaService.EnableTotpAsync();
            Console.WriteLine(
                $"Enable TOTP: ResultCode={enableTotpResult.ResultCode}, Secret={enableTotpResult.Secret}"
            );
            Console.WriteLine($"  Setup URI: {enableTotpResult.SetupUri}");
            if (enableTotpResult.RecoveryCodes != null)
            {
                Console.WriteLine(
                    $"  Recovery codes: {string.Join(", ", enableTotpResult.RecoveryCodes)}"
                );
            }

            // Confirm TOTP with a generated code
            var totpProvider = host.Services.GetRequiredService<ITotpProvider>();
            var totpCode = totpProvider.GenerateCode(enableTotpResult.Secret!);
            var confirmTotpResult = await mfaService.ConfirmTotpAsync(
                new ConfirmTotpRequest(totpCode)
            );
            Console.WriteLine($"Confirm TOTP: {confirmTotpResult.ResultCode}");

            // Check TOTP status
            var totpStatus = await mfaService.GetTotpStatusAsync();
            Console.WriteLine(
                $"TOTP Status: Enabled={totpStatus.IsEnabled}, RemainingCodes={totpStatus.RemainingRecoveryCodes}"
            );

            // Sign in — now requires MFA
            var mfaSignInResult = await authenticationService.SignInAsync(
                new SignInRequest("user1", "admin", TEST_DEVICE_ID)
            );
            Console.WriteLine(
                $"Sign in with MFA: ResultCode={mfaSignInResult.ResultCode}, HasChallenge={mfaSignInResult.MfaChallengeToken != null}"
            );

            // Verify MFA with a TOTP code
            var mfaCode = totpProvider.GenerateCode(enableTotpResult.Secret!);
            var verifyMfaResult = await authenticationService.VerifyMfaAsync(
                new VerifyMfaRequest(mfaSignInResult.MfaChallengeToken!, mfaCode)
            );
            Console.WriteLine(
                $"Verify MFA: ResultCode={verifyMfaResult.ResultCode}, HasToken={verifyMfaResult.AuthToken != null}"
            );

            // Switch back to the account context
            switchAccountResult = await authenticationService.SwitchAccountAsync(
                new SwitchAccountRequest(registerAccountResult.CreatedAccountId)
            );

            // Regenerate recovery codes
            var regenResult = await mfaService.RegenerateTotpRecoveryCodesAsync();
            Console.WriteLine($"Regenerate codes: {regenResult.ResultCode}");
            if (regenResult.RecoveryCodes != null)
            {
                Console.WriteLine($"  New codes: {string.Join(", ", regenResult.RecoveryCodes)}");
            }

            // Disable TOTP
            var disableCode = totpProvider.GenerateCode(enableTotpResult.Secret!);
            var disableTotpResult = await mfaService.DisableTotpAsync(
                new DisableTotpRequest(disableCode)
            );
            Console.WriteLine($"Disable TOTP: {disableTotpResult.ResultCode}");

            // ========= AUTH METHOD MANAGEMENT ==========

            // Check auth methods (user1 has basic auth only — no Google in ConsoleTest)
            var authMethods = await googleAuthService.GetAuthMethodsAsync();
            Console.WriteLine(
                $"Auth methods: HasBasicAuth={authMethods.HasBasicAuth}, HasGoogleAuth={authMethods.HasGoogleAuth}"
            );

            // Try to remove basic auth — should fail (last auth method)
            var deregisterBasicAuthResult = await deregistrationService.DeregisterBasicAuthAsync();
            Console.WriteLine(
                $"Deregister basic auth (last method): {deregisterBasicAuthResult.ResultCode}"
            );
            // Note: RegisterUserWithGoogleAsync and full Google flows require a real Google
            // OIDC endpoint and are not demoed here. See DevTools commands for token-based testing.

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
                new SignInRequest("user2", "password2", TEST_DEVICE_ID)
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
