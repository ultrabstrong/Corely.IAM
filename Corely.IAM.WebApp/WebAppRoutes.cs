namespace Corely.IAM.WebApp;

public static class WebAppRoutes
{
    public const string ForgotPassword = "/forgot-password";
    public const string PasswordRecoveryPreview = "/forgot-password-preview";
    public const string ResetPassword = "/reset-password";

    public const string Home = "/";
    public const string Profile = "/profile";
    public const string AcceptInvitation = "/accept-invitation";
    public const string PlatformSettings = "/platform-settings";
    public const string PlatformAuditSettings = "/platform-settings/audit";
    public const string PlatformSecuritySettings = "/platform-settings/security";

    public static string Account(Guid accountId) => $"/accounts/{accountId}";

    public static string Users(Guid accountId) => $"{Account(accountId)}/users";

    public static string User(Guid accountId, Guid userId) => $"{Users(accountId)}/{userId}";

    public static string Groups(Guid accountId) => $"{Account(accountId)}/groups";

    public static string Group(Guid accountId, Guid groupId) => $"{Groups(accountId)}/{groupId}";

    public static string Roles(Guid accountId) => $"{Account(accountId)}/roles";

    public static string Role(Guid accountId, Guid roleId) => $"{Roles(accountId)}/{roleId}";

    public static string Permissions(Guid accountId) => $"{Account(accountId)}/permissions";

    public static string ApiKeys(Guid accountId) => $"{Account(accountId)}/api-keys";

    public static string ApiKey(Guid accountId, Guid apiKeyId) =>
        $"{ApiKeys(accountId)}/{apiKeyId}";

    public static string Permission(Guid accountId, Guid permissionId) =>
        $"{Permissions(accountId)}/{permissionId}";

    public static string Audit(Guid accountId) => $"{Account(accountId)}/audit";

    public static string AuditEntry(Guid accountId, Guid entryId) =>
        $"{Audit(accountId)}/{entryId}";
}
