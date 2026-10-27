using Corely.IAM.Accounts.Models;
using Corely.IAM.ApiKeys.Enums;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Audits.Enums;
using Corely.IAM.Audits.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Authorization;

internal static class CrossAccountCases
{
    private static T Get<T>(IServiceProvider services)
        where T : notnull => services.GetRequiredService<T>();

    public static readonly IReadOnlyDictionary<string, string> Exemptions = new Dictionary<
        string,
        string
    >
    {
        [$"{nameof(IRegistrationService)}.{nameof(IRegistrationService.RegisterUserAsync)}"] =
            "Creates a new user from its own credentials; names no account",
        [
            $"{nameof(IRegistrationService)}.{nameof(IRegistrationService.RegisterUserWithGoogleAsync)}"
        ] = "Creates a new user from a Google token; names no account",
        [$"{nameof(IRegistrationService)}.{nameof(IRegistrationService.RegisterPasswordAsync)}"] =
            "Sets the caller's own password",
        [$"{nameof(IDeregistrationService)}.{nameof(IDeregistrationService.DeregisterUserAsync)}"] =
            "Deletes the caller themselves",
        [
            $"{nameof(IDeregistrationService)}.{nameof(IDeregistrationService.DeregisterBasicAuthAsync)}"
        ] = "Removes the caller's own password",
        [
            $"{nameof(IModificationService)}.{nameof(IModificationService.RotateCurrentUserKeyAsync)}"
        ] = "Rotates the caller's own key",
        [$"{nameof(IModificationService)}.{nameof(IModificationService.ModifyPasswordAsync)}"] =
            "Changes the caller's own password",
        [
            $"{nameof(IRetrievalService)}.{nameof(IRetrievalService.GetUserSymmetricEncryptionProviderAsync)}"
        ] = "Returns the caller's own key",
        [
            $"{nameof(IRetrievalService)}.{nameof(IRetrievalService.GetUserAsymmetricEncryptionProviderAsync)}"
        ] = "Returns the caller's own key",
        [
            $"{nameof(IRetrievalService)}.{nameof(IRetrievalService.GetUserAsymmetricSignatureProviderAsync)}"
        ] = "Returns the caller's own key",
        [$"{nameof(IInvitationService)}.{nameof(IInvitationService.AcceptInvitationAsync)}"] =
            "The invitation token is the credential; whoever holds it may accept",
        [$"{nameof(IAuthenticationService)}.{nameof(IAuthenticationService.SignInAsync)}"] =
            "Authenticates with the credentials it is given",
        [
            $"{nameof(IAuthenticationService)}.{nameof(IAuthenticationService.SignInWithGoogleAsync)}"
        ] = "Authenticates with the Google token it is given",
        [$"{nameof(IAuthenticationService)}.{nameof(IAuthenticationService.VerifyMfaAsync)}"] =
            "Completes a challenge with the challenge token it is given",
        [
            $"{nameof(IAuthenticationService)}.{nameof(IAuthenticationService.SignInWithApiKeyAsync)}"
        ] = "Authenticates with the API key it is given",
        [$"{nameof(IAuthenticationService)}.{nameof(IAuthenticationService.RenewAuthTokenAsync)}"] =
            "Renews with the refresh token it is given",
        [
            $"{nameof(IAuthenticationService)}.{nameof(IAuthenticationService.RevokeOtherSessionsAsync)}"
        ] = "Ends the caller's own other sessions",
        [$"{nameof(IAuthenticationService)}.{nameof(IAuthenticationService.SignOutAllAsync)}"] =
            "Ends the caller's own sessions",
        [
            $"{nameof(IAuthenticationService)}.{nameof(IAuthenticationService.AuthenticateWithTokenAsync)}"
        ] = "Validates the token it is given",
        [
            $"{nameof(IAuthenticationService)}.{nameof(IAuthenticationService.AuthenticateAsSystem)}"
        ] = "Sets system context in process; reachable only from host code",
        [$"{nameof(IGoogleAuthService)}.{nameof(IGoogleAuthService.LinkGoogleAuthAsync)}"] =
            "Links Google to the caller's own user",
        [$"{nameof(IGoogleAuthService)}.{nameof(IGoogleAuthService.UnlinkGoogleAuthAsync)}"] =
            "Unlinks Google from the caller's own user",
        [$"{nameof(IGoogleAuthService)}.{nameof(IGoogleAuthService.GetAuthMethodsAsync)}"] =
            "Reads the caller's own sign in methods",
        [$"{nameof(IMfaService)}.{nameof(IMfaService.EnableTotpAsync)}"] =
            "Acts on the caller's own two factor",
        [$"{nameof(IMfaService)}.{nameof(IMfaService.ConfirmTotpAsync)}"] =
            "Acts on the caller's own two factor",
        [$"{nameof(IMfaService)}.{nameof(IMfaService.DisableTotpAsync)}"] =
            "Acts on the caller's own two factor",
        [$"{nameof(IMfaService)}.{nameof(IMfaService.RegenerateTotpRecoveryCodesAsync)}"] =
            "Acts on the caller's own two factor",
        [$"{nameof(IMfaService)}.{nameof(IMfaService.GetTotpStatusAsync)}"] =
            "Reads the caller's own two factor",
        [
            $"{nameof(IPasswordRecoveryService)}.{nameof(IPasswordRecoveryService.RequestPasswordRecoveryAsync)}"
        ] = "Signed out flow keyed by email; names no account",
        [
            $"{nameof(IPasswordRecoveryService)}.{nameof(IPasswordRecoveryService.ValidatePasswordRecoveryTokenAsync)}"
        ] = "Signed out flow; the recovery token is the credential",
        [
            $"{nameof(IPasswordRecoveryService)}.{nameof(IPasswordRecoveryService.ResetPasswordWithRecoveryAsync)}"
        ] = "Signed out flow; the recovery token is the credential",
    };

    public static readonly IReadOnlyDictionary<string, Func<CrossAccountAttack, Task>> Cases =
        new Dictionary<string, Func<CrossAccountAttack, Task>>
        {
            [
                $"{nameof(IRegistrationService)}.{nameof(IRegistrationService.RegisterAccountAsync)}"
            ] = a =>
                a.WritesNothingAsync(s =>
                    Get<IRegistrationService>(s)
                        .RegisterAccountAsync(new RegisterAccountRequest("Taken", a.OwnerUserId))
                ),
            [$"{nameof(IRegistrationService)}.{nameof(IRegistrationService.RegisterGroupAsync)}"] =
                a =>
                    a.WritesNothingNamingTheVictimAsync(
                        (s, account) =>
                            Get<IRegistrationService>(s)
                                .RegisterGroupAsync(new RegisterGroupRequest("Planted", account))
                    ),
            [$"{nameof(IRegistrationService)}.{nameof(IRegistrationService.RegisterRoleAsync)}"] =
                a =>
                    a.WritesNothingNamingTheVictimAsync(
                        (s, account) =>
                            Get<IRegistrationService>(s)
                                .RegisterRoleAsync(new RegisterRoleRequest("Planted", account))
                    ),
            [
                $"{nameof(IRegistrationService)}.{nameof(IRegistrationService.RegisterPermissionAsync)}"
            ] = a =>
                a.WritesNothingNamingTheVictimAsync(
                    (s, account) =>
                        Get<IRegistrationService>(s)
                            .RegisterPermissionAsync(
                                new RegisterPermissionRequest(
                                    account,
                                    Role.RESOURCE_TYPE,
                                    Guid.Empty,
                                    Read: true
                                )
                            )
                ),
            [$"{nameof(IRegistrationService)}.{nameof(IRegistrationService.RegisterApiKeyAsync)}"] =
                a =>
                    a.WritesNothingNamingTheVictimAsync(
                        (s, account) =>
                            Get<IRegistrationService>(s)
                                .RegisterApiKeyAsync(new RegisterApiKeyRequest("planted", account))
                    ),
            [
                $"{nameof(IRegistrationService)}.{nameof(IRegistrationService.RegisterUserWithAccountAsync)}"
            ] = a =>
                a.WritesNothingNamingTheVictimAsync(
                    (s, account) =>
                        Get<IRegistrationService>(s)
                            .RegisterUserWithAccountAsync(
                                new RegisterUserWithAccountRequest(a.OutsiderUserId, account)
                            )
                ),
            [
                $"{nameof(IRegistrationService)}.{nameof(IRegistrationService.RegisterUsersWithGroupAsync)}"
            ] = a =>
                a.WritesNothingAsync(
                    async (s, account) =>
                    {
                        var registration = Get<IRegistrationService>(s);
                        await registration.RegisterUsersWithGroupAsync(
                            new RegisterUsersWithGroupRequest(
                                [a.DirectMemberUserId],
                                a.EditorGroupId,
                                account
                            )
                        );
                        await registration.RegisterUsersWithGroupAsync(
                            new RegisterUsersWithGroupRequest(
                                [a.OutsiderUserId],
                                a.EditorGroupId,
                                account
                            )
                        );
                    }
                ),
            [
                $"{nameof(IRegistrationService)}.{nameof(IRegistrationService.RegisterRolesWithGroupAsync)}"
            ] = a =>
                a.WritesNothingAsync(
                    async (s, account) =>
                    {
                        var registration = Get<IRegistrationService>(s);
                        await registration.RegisterRolesWithGroupAsync(
                            new RegisterRolesWithGroupRequest(
                                [a.ReaderRoleId],
                                a.EditorGroupId,
                                account
                            )
                        );
                        await registration.RegisterRolesWithGroupAsync(
                            new RegisterRolesWithGroupRequest(
                                [a.OutsiderRoleId],
                                a.EditorGroupId,
                                account
                            )
                        );
                        await registration.RegisterRolesWithGroupAsync(
                            new RegisterRolesWithGroupRequest(
                                [a.ReaderRoleId],
                                a.OutsiderGroupId,
                                account
                            )
                        );
                    }
                ),
            [
                $"{nameof(IRegistrationService)}.{nameof(IRegistrationService.RegisterRolesWithUserAsync)}"
            ] = a =>
                a.WritesNothingAsync(
                    async (s, account) =>
                    {
                        var registration = Get<IRegistrationService>(s);
                        await registration.RegisterRolesWithUserAsync(
                            new RegisterRolesWithUserRequest(
                                [a.EditorRoleId],
                                a.DirectMemberUserId,
                                account
                            )
                        );
                        await registration.RegisterRolesWithUserAsync(
                            new RegisterRolesWithUserRequest(
                                [a.ReaderRoleId],
                                a.OutsiderUserId,
                                account
                            )
                        );
                        await registration.RegisterRolesWithUserAsync(
                            new RegisterRolesWithUserRequest(
                                [a.OutsiderRoleId],
                                a.DirectMemberUserId,
                                account
                            )
                        );
                    }
                ),
            [
                $"{nameof(IRegistrationService)}.{nameof(IRegistrationService.RegisterRolesWithApiKeyAsync)}"
            ] = a =>
                a.WritesNothingAsync(
                    async (s, account) =>
                    {
                        var registration = Get<IRegistrationService>(s);
                        await registration.RegisterRolesWithApiKeyAsync(
                            new RegisterRolesWithApiKeyRequest(
                                [a.ReaderRoleId],
                                a.BareApiKeyId,
                                account
                            )
                        );
                        await registration.RegisterRolesWithApiKeyAsync(
                            new RegisterRolesWithApiKeyRequest(
                                [a.OutsiderRoleId],
                                a.BareApiKeyId,
                                account
                            )
                        );
                        await registration.RegisterRolesWithApiKeyAsync(
                            new RegisterRolesWithApiKeyRequest(
                                [a.ReaderRoleId],
                                a.OutsiderApiKeyId,
                                account
                            )
                        );
                    }
                ),
            [
                $"{nameof(IRegistrationService)}.{nameof(IRegistrationService.RegisterApiKeysWithGroupAsync)}"
            ] = a =>
                a.WritesNothingAsync(
                    async (s, account) =>
                    {
                        var registration = Get<IRegistrationService>(s);
                        await registration.RegisterApiKeysWithGroupAsync(
                            new RegisterApiKeysWithGroupRequest(
                                [a.BareApiKeyId],
                                a.EditorGroupId,
                                account
                            )
                        );
                        await registration.RegisterApiKeysWithGroupAsync(
                            new RegisterApiKeysWithGroupRequest(
                                [a.OutsiderApiKeyId],
                                a.EditorGroupId,
                                account
                            )
                        );
                        await registration.RegisterApiKeysWithGroupAsync(
                            new RegisterApiKeysWithGroupRequest(
                                [a.BareApiKeyId],
                                a.OutsiderGroupId,
                                account
                            )
                        );
                    }
                ),
            [
                $"{nameof(IRegistrationService)}.{nameof(IRegistrationService.RegisterPermissionsWithRoleAsync)}"
            ] = a =>
                a.WritesNothingAsync(
                    async (s, account) =>
                    {
                        var registration = Get<IRegistrationService>(s);
                        await registration.RegisterPermissionsWithRoleAsync(
                            new RegisterPermissionsWithRoleRequest(
                                [a.VictimPermissionId],
                                a.EditorRoleId,
                                account
                            )
                        );
                        await registration.RegisterPermissionsWithRoleAsync(
                            new RegisterPermissionsWithRoleRequest(
                                [a.OutsiderPermissionId],
                                a.EditorRoleId,
                                account
                            )
                        );
                        await registration.RegisterPermissionsWithRoleAsync(
                            new RegisterPermissionsWithRoleRequest(
                                [a.VictimPermissionId],
                                a.OutsiderRoleId,
                                account
                            )
                        );
                    }
                ),
            [
                $"{nameof(IDeregistrationService)}.{nameof(IDeregistrationService.DeregisterAccountAsync)}"
            ] = a =>
                a.WritesNothingAsync(s =>
                    Get<IDeregistrationService>(s)
                        .DeregisterAccountAsync(new DeregisterAccountRequest(a.VictimAccountId))
                ),
            [
                $"{nameof(IDeregistrationService)}.{nameof(IDeregistrationService.DeregisterGroupAsync)}"
            ] = a =>
                a.WritesNothingAsync(
                    (s, account) =>
                        Get<IDeregistrationService>(s)
                            .DeregisterGroupAsync(
                                new DeregisterGroupRequest(a.EditorGroupId, account)
                            )
                ),
            [
                $"{nameof(IDeregistrationService)}.{nameof(IDeregistrationService.DeregisterRoleAsync)}"
            ] = a =>
                a.WritesNothingAsync(
                    (s, account) =>
                        Get<IDeregistrationService>(s)
                            .DeregisterRoleAsync(new DeregisterRoleRequest(a.ReaderRoleId, account))
                ),
            [
                $"{nameof(IDeregistrationService)}.{nameof(IDeregistrationService.DeregisterApiKeyAsync)}"
            ] = a =>
                a.WritesNothingAsync(
                    (s, account) =>
                        Get<IDeregistrationService>(s)
                            .DeregisterApiKeyAsync(
                                new DeregisterApiKeyRequest(a.HeldApiKeyId, account)
                            )
                ),
            [
                $"{nameof(IDeregistrationService)}.{nameof(IDeregistrationService.DeregisterPermissionAsync)}"
            ] = a =>
                a.WritesNothingAsync(
                    (s, account) =>
                        Get<IDeregistrationService>(s)
                            .DeregisterPermissionAsync(
                                new DeregisterPermissionRequest(a.VictimPermissionId, account)
                            )
                ),
            [
                $"{nameof(IDeregistrationService)}.{nameof(IDeregistrationService.DeregisterUserFromAccountAsync)}"
            ] = a =>
                a.WritesNothingAsync(
                    (s, account) =>
                        Get<IDeregistrationService>(s)
                            .DeregisterUserFromAccountAsync(
                                new DeregisterUserFromAccountRequest(a.DirectMemberUserId, account)
                            )
                ),
            [
                $"{nameof(IDeregistrationService)}.{nameof(IDeregistrationService.DeregisterUsersFromGroupAsync)}"
            ] = a =>
                a.WritesNothingAsync(
                    (s, account) =>
                        Get<IDeregistrationService>(s)
                            .DeregisterUsersFromGroupAsync(
                                new DeregisterUsersFromGroupRequest(
                                    [a.GroupMemberUserId],
                                    a.EditorGroupId,
                                    account
                                )
                            )
                ),
            [
                $"{nameof(IDeregistrationService)}.{nameof(IDeregistrationService.DeregisterRolesFromGroupAsync)}"
            ] = a =>
                a.WritesNothingAsync(
                    (s, account) =>
                        Get<IDeregistrationService>(s)
                            .DeregisterRolesFromGroupAsync(
                                new DeregisterRolesFromGroupRequest(
                                    [a.EditorRoleId],
                                    a.EditorGroupId,
                                    account
                                )
                            )
                ),
            [
                $"{nameof(IDeregistrationService)}.{nameof(IDeregistrationService.DeregisterRolesFromUserAsync)}"
            ] = a =>
                a.WritesNothingAsync(
                    (s, account) =>
                        Get<IDeregistrationService>(s)
                            .DeregisterRolesFromUserAsync(
                                new DeregisterRolesFromUserRequest(
                                    [a.ReaderRoleId],
                                    a.DirectMemberUserId,
                                    account
                                )
                            )
                ),
            [
                $"{nameof(IDeregistrationService)}.{nameof(IDeregistrationService.DeregisterRolesFromApiKeyAsync)}"
            ] = a =>
                a.WritesNothingAsync(
                    (s, account) =>
                        Get<IDeregistrationService>(s)
                            .DeregisterRolesFromApiKeyAsync(
                                new DeregisterRolesFromApiKeyRequest(
                                    [a.ReaderRoleId],
                                    a.HeldApiKeyId,
                                    account
                                )
                            )
                ),
            [
                $"{nameof(IDeregistrationService)}.{nameof(IDeregistrationService.DeregisterApiKeysFromGroupAsync)}"
            ] = a =>
                a.WritesNothingAsync(
                    (s, account) =>
                        Get<IDeregistrationService>(s)
                            .DeregisterApiKeysFromGroupAsync(
                                new DeregisterApiKeysFromGroupRequest(
                                    [a.HeldApiKeyId],
                                    a.EditorGroupId,
                                    account
                                )
                            )
                ),
            [
                $"{nameof(IDeregistrationService)}.{nameof(IDeregistrationService.DeregisterPermissionsFromRoleAsync)}"
            ] = a =>
                a.WritesNothingAsync(
                    (s, account) =>
                        Get<IDeregistrationService>(s)
                            .DeregisterPermissionsFromRoleAsync(
                                new DeregisterPermissionsFromRoleRequest(
                                    [a.VictimPermissionId],
                                    a.ReaderRoleId,
                                    account
                                )
                            )
                ),
            [$"{nameof(IModificationService)}.{nameof(IModificationService.ModifyAccountAsync)}"] =
                a =>
                    a.WritesNothingAsync(s =>
                        Get<IModificationService>(s)
                            .ModifyAccountAsync(
                                new UpdateAccountRequest(a.VictimAccountId, "Hijacked")
                            )
                    ),
            [$"{nameof(IModificationService)}.{nameof(IModificationService.ModifyUserAsync)}"] =
                a =>
                    a.WritesNothingAsync(s =>
                        Get<IModificationService>(s)
                            .ModifyUserAsync(
                                new UpdateUserRequest(
                                    a.DirectMemberUserId,
                                    "hijacked",
                                    "hijacked@example.com"
                                )
                            )
                    ),
            [$"{nameof(IModificationService)}.{nameof(IModificationService.ModifyGroupAsync)}"] =
                a =>
                    a.WritesNothingAsync(
                        (s, account) =>
                            Get<IModificationService>(s)
                                .ModifyGroupAsync(
                                    new UpdateGroupRequest(
                                        a.EditorGroupId,
                                        account,
                                        "Hijacked",
                                        null
                                    )
                                )
                    ),
            [$"{nameof(IModificationService)}.{nameof(IModificationService.ModifyRoleAsync)}"] =
                a =>
                    a.WritesNothingAsync(
                        (s, account) =>
                            Get<IModificationService>(s)
                                .ModifyRoleAsync(
                                    new UpdateRoleRequest(a.ReaderRoleId, account, "Hijacked", null)
                                )
                    ),
            [
                $"{nameof(IModificationService)}.{nameof(IModificationService.ModifyPermissionAsync)}"
            ] = a =>
                a.WritesNothingAsync(
                    (s, account) =>
                        Get<IModificationService>(s)
                            .ModifyPermissionAsync(
                                new UpdatePermissionRequest(
                                    a.VictimPermissionId,
                                    account,
                                    "Hijacked"
                                )
                            )
                ),
            [$"{nameof(IModificationService)}.{nameof(IModificationService.ModifyApiKeyAsync)}"] =
                a =>
                    a.WritesNothingAsync(
                        (s, account) =>
                            Get<IModificationService>(s)
                                .ModifyApiKeyAsync(
                                    new UpdateApiKeyRequest(
                                        a.HeldApiKeyId,
                                        account,
                                        "hijacked",
                                        null
                                    )
                                )
                    ),
            [
                $"{nameof(IModificationService)}.{nameof(IModificationService.RotateApiKeySecretAsync)}"
            ] = a =>
                a.WritesNothingAsync(
                    (s, account) =>
                        Get<IModificationService>(s)
                            .RotateApiKeySecretAsync(
                                new RotateApiKeySecretRequest(
                                    a.HeldApiKeyId,
                                    account,
                                    ApiKeySlot.Primary
                                )
                            )
                ),
            [
                $"{nameof(IModificationService)}.{nameof(IModificationService.RotateAccountKeyAsync)}"
            ] = a =>
                a.WritesNothingAsync(s =>
                    Get<IModificationService>(s)
                        .RotateAccountKeyAsync(
                            new RotateAccountKeyRequest(
                                a.VictimAccountId,
                                KeyType.SymmetricEncryption
                            )
                        )
                ),
            [$"{nameof(IRetrievalService)}.{nameof(IRetrievalService.ListPermissionsAsync)}"] = a =>
                a.LeaksNothingAsync(
                    (s, account) =>
                        Get<IRetrievalService>(s)
                            .ListPermissionsAsync(new ListPermissionsRequest(account))
                ),
            [$"{nameof(IRetrievalService)}.{nameof(IRetrievalService.GetPermissionAsync)}"] = a =>
                a.LeaksNothingAsync(s =>
                    Get<IRetrievalService>(s).GetPermissionAsync(a.VictimPermissionId, true)
                ),
            [$"{nameof(IRetrievalService)}.{nameof(IRetrievalService.ListGroupsAsync)}"] = a =>
                a.LeaksNothingAsync(
                    (s, account) =>
                        Get<IRetrievalService>(s).ListGroupsAsync(new ListGroupsRequest(account))
                ),
            [$"{nameof(IRetrievalService)}.{nameof(IRetrievalService.GetGroupAsync)}"] = a =>
                a.LeaksNothingAsync(s =>
                    Get<IRetrievalService>(s).GetGroupAsync(a.EditorGroupId, true)
                ),
            [$"{nameof(IRetrievalService)}.{nameof(IRetrievalService.ListRolesAsync)}"] = a =>
                a.LeaksNothingAsync(
                    (s, account) =>
                        Get<IRetrievalService>(s).ListRolesAsync(new ListRolesRequest(account))
                ),
            [$"{nameof(IRetrievalService)}.{nameof(IRetrievalService.GetRoleAsync)}"] = a =>
                a.LeaksNothingAsync(s =>
                    Get<IRetrievalService>(s).GetRoleAsync(a.ReaderRoleId, true)
                ),
            [$"{nameof(IRetrievalService)}.{nameof(IRetrievalService.ListApiKeysAsync)}"] = a =>
                a.LeaksNothingAsync(
                    (s, account) =>
                        Get<IRetrievalService>(s).ListApiKeysAsync(new ListApiKeysRequest(account))
                ),
            [$"{nameof(IRetrievalService)}.{nameof(IRetrievalService.GetApiKeyAsync)}"] = a =>
                a.LeaksNothingAsync(s =>
                    Get<IRetrievalService>(s).GetApiKeyAsync(a.HeldApiKeyId, true)
                ),
            [$"{nameof(IRetrievalService)}.{nameof(IRetrievalService.ListUsersAsync)}"] = a =>
                a.LeaksNothingAsync(
                    (s, account) =>
                        Get<IRetrievalService>(s).ListUsersAsync(new ListUsersRequest(account))
                ),
            [$"{nameof(IRetrievalService)}.{nameof(IRetrievalService.GetUserAsync)}"] = a =>
                a.LeaksNothingAsync(s =>
                    Get<IRetrievalService>(s).GetUserAsync(a.DirectMemberUserId, true)
                ),
            [$"{nameof(IRetrievalService)}.{nameof(IRetrievalService.ListAccountsAsync)}"] = a =>
                a.LeaksNothingAsync(s =>
                    Get<IRetrievalService>(s).ListAccountsAsync(new ListAccountsRequest())
                ),
            [$"{nameof(IRetrievalService)}.{nameof(IRetrievalService.GetAccountAsync)}"] = a =>
                a.LeaksNothingAsync(s =>
                    Get<IRetrievalService>(s).GetAccountAsync(a.VictimAccountId, true)
                ),
            [
                $"{nameof(IRetrievalService)}.{nameof(IRetrievalService.GetAccountSymmetricEncryptionProviderAsync)}"
            ] = a =>
                a.RefusedAsync(
                    s =>
                        Get<IRetrievalService>(s)
                            .GetAccountSymmetricEncryptionProviderAsync(a.VictimAccountId),
                    r => r.Item != null
                ),
            [
                $"{nameof(IRetrievalService)}.{nameof(IRetrievalService.GetAccountAsymmetricEncryptionProviderAsync)}"
            ] = a =>
                a.RefusedAsync(
                    s =>
                        Get<IRetrievalService>(s)
                            .GetAccountAsymmetricEncryptionProviderAsync(a.VictimAccountId),
                    r => r.Item != null
                ),
            [
                $"{nameof(IRetrievalService)}.{nameof(IRetrievalService.GetAccountAsymmetricSignatureProviderAsync)}"
            ] = a =>
                a.RefusedAsync(
                    s =>
                        Get<IRetrievalService>(s)
                            .GetAccountAsymmetricSignatureProviderAsync(a.VictimAccountId),
                    r => r.Item != null
                ),
            [$"{nameof(IInvitationService)}.{nameof(IInvitationService.CreateInvitationAsync)}"] =
                a =>
                    a.WritesNothingAsync(s =>
                        Get<IInvitationService>(s)
                            .CreateInvitationAsync(
                                new CreateInvitationRequest(
                                    a.VictimAccountId,
                                    "planted@example.com",
                                    null,
                                    3600
                                )
                            )
                    ),
            [$"{nameof(IInvitationService)}.{nameof(IInvitationService.RevokeInvitationAsync)}"] =
                a =>
                    a.WritesNothingAsync(
                        (s, account) =>
                            Get<IInvitationService>(s)
                                .RevokeInvitationAsync(
                                    new RevokeInvitationRequest(account, a.VictimInvitationId)
                                )
                    ),
            [$"{nameof(IInvitationService)}.{nameof(IInvitationService.ListInvitationsAsync)}"] =
                a =>
                    a.LeaksNothingAsync(
                        (s, account) =>
                            Get<IInvitationService>(s)
                                .ListInvitationsAsync(new ListInvitationsRequest(account))
                    ),
            [$"{nameof(IAuditService)}.{nameof(IAuditService.ListEntriesAsync)}"] = a =>
                a.LeaksNothingAsync(
                    (s, account) =>
                        Get<IAuditService>(s)
                            .ListEntriesAsync(
                                new ListAuditEntriesRequest(
                                    new AuditEntryFilter { AccountId = account }
                                )
                            )
                ),
            [$"{nameof(IAuditService)}.{nameof(IAuditService.GetEntryAsync)}"] = a =>
                a.LeaksNothingAsync(s => Get<IAuditService>(s).GetEntryAsync(a.VictimAuditEntryId)),
            [$"{nameof(IAuditService)}.{nameof(IAuditService.ExportEntriesAsync)}"] = a =>
                a.LeaksNothingAsync(
                    (s, account) =>
                        Get<IAuditService>(s)
                            .ExportEntriesAsync(new AuditEntryFilter { AccountId = account })
                ),
            [$"{nameof(IAuditService)}.{nameof(IAuditService.ListAuditAccountsAsync)}"] = a =>
                a.LeaksNothingAsync(s =>
                    Get<IAuditService>(s).ListAuditAccountsAsync(AuthAction.Read)
                ),
            [$"{nameof(IAuditService)}.{nameof(IAuditService.CountPurgeableEntriesAsync)}"] = a =>
                a.RefusedAsync(
                    s =>
                        Get<IAuditService>(s)
                            .CountPurgeableEntriesAsync(
                                new PurgeAuditEntriesRequest(a.VictimAccountId)
                            ),
                    r => r.ResultCode == PurgeAuditEntriesResultCode.Success
                ),
            [$"{nameof(IAuditService)}.{nameof(IAuditService.PurgeEntriesAsync)}"] = a =>
                a.WritesNothingAsync(s =>
                    Get<IAuditService>(s)
                        .PurgeEntriesAsync(new PurgeAuditEntriesRequest(a.VictimAccountId))
                ),
            [$"{nameof(IAuditService)}.{nameof(IAuditService.GetAccountSettingsAsync)}"] = a =>
                a.RefusedAsync(
                    s => Get<IAuditService>(s).GetAccountSettingsAsync(a.VictimAccountId),
                    r => r.ResultCode == RetrieveResultCode.Success
                ),
            [$"{nameof(IAuditService)}.{nameof(IAuditService.UpdateAccountSettingsAsync)}"] = a =>
                a.WritesNothingAsync(s =>
                    Get<IAuditService>(s)
                        .UpdateAccountSettingsAsync(
                            new UpdateAccountAuditSettingsRequest(
                                a.VictimAccountId,
                                AuditActions.None,
                                AuditActions.None,
                                1
                            )
                        )
                ),
            [$"{nameof(IAuditService)}.{nameof(IAuditService.GetPlatformSettingsAsync)}"] = a =>
                a.RefusedAsync(
                    s => Get<IAuditService>(s).GetPlatformSettingsAsync(),
                    r => r.ResultCode == RetrieveResultCode.Success
                ),
            [$"{nameof(IAuditService)}.{nameof(IAuditService.UpdatePlatformSettingsAsync)}"] = a =>
                a.WritesNothingAsync(s =>
                    Get<IAuditService>(s).UpdatePlatformSettingsAsync(PlatformSettings.Default)
                ),
            [$"{nameof(IAuditService)}.{nameof(IAuditService.DeleteExpiredEntriesAsync)}"] = a =>
                a.WritesNothingAsync(s => Get<IAuditService>(s).DeleteExpiredEntriesAsync()),
            [$"{nameof(IPlatformService)}.{nameof(IPlatformService.BootstrapPlatformAsync)}"] = a =>
                a.WritesNothingAsync(s =>
                    Get<IPlatformService>(s)
                        .BootstrapPlatformAsync(
                            new BootstrapPlatformRequest("Seized", "seizer", "seizer@example.com")
                        )
                ),
            [
                $"{nameof(IPlatformService)}.{nameof(IPlatformService.CompletePlatformOwnerPermissionsAsync)}"
            ] = a =>
                a.WritesNothingAsync(s =>
                    Get<IPlatformService>(s).CompletePlatformOwnerPermissionsAsync()
                ),
            [$"{nameof(IPlatformService)}.{nameof(IPlatformService.GetSecuritySettingsAsync)}"] =
                a =>
                    a.RefusedAsync(
                        s => Get<IPlatformService>(s).GetSecuritySettingsAsync(),
                        r => r.ResultCode == RetrieveResultCode.Success
                    ),
            [$"{nameof(IPlatformService)}.{nameof(IPlatformService.UpdateSecuritySettingsAsync)}"] =
                a =>
                    a.WritesNothingAsync(s =>
                        Get<IPlatformService>(s)
                            .UpdateSecuritySettingsAsync(
                                SecurityPolicy.Default with
                                {
                                    MaxLoginAttempts = SecurityPolicy.MAX_LOGIN_ATTEMPTS_MAX,
                                }
                            )
                    ),
            [
                $"{nameof(IAuthenticationService)}.{nameof(IAuthenticationService.SwitchAccountAsync)}"
            ] = a =>
                a.RefusedAsync(
                    s =>
                        Get<IAuthenticationService>(s)
                            .SwitchAccountAsync(new SwitchAccountRequest(a.VictimAccountId)),
                    r => r.ResultCode == SignInResultCode.Success
                ),
            [
                $"{nameof(IAuthenticationService)}.{nameof(IAuthenticationService.ListSessionsAsync)}"
            ] = a => a.LeaksNothingAsync(s => Get<IAuthenticationService>(s).ListSessionsAsync()),
            [
                $"{nameof(IAuthenticationService)}.{nameof(IAuthenticationService.RevokeSessionAsync)}"
            ] = a =>
                a.WritesNothingAsync(s =>
                    Get<IAuthenticationService>(s)
                        .RevokeSessionAsync(new RevokeSessionRequest(a.OwnerSessionId))
                ),
            [$"{nameof(IAuthenticationService)}.{nameof(IAuthenticationService.SignOutAsync)}"] =
                a =>
                    a.WritesNothingAsync(s =>
                        Get<IAuthenticationService>(s)
                            .SignOutAsync(new SignOutRequest(a.OwnerTokenId.ToString()))
                    ),
            [
                $"{nameof(IAuthenticationService)}.{nameof(IAuthenticationService.DeleteExpiredAuthTokensAsync)}"
            ] = a =>
                a.WritesNothingAsync(s =>
                    Get<IAuthenticationService>(s).DeleteExpiredAuthTokensAsync()
                ),
        };
}
