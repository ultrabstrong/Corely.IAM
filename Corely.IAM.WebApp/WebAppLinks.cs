using Corely.IAM.Web;

namespace Corely.IAM.WebApp;

internal sealed class WebAppLinks : IIamWebLinks
{
    public string Dashboard() => WebAppRoutes.Home;

    public string Profile() => WebAppRoutes.Profile;

    public string AcceptInvitation() => WebAppRoutes.AcceptInvitation;

    public string Account(Guid accountId) => WebAppRoutes.Account(accountId);

    public string Users(Guid accountId) => WebAppRoutes.Users(accountId);

    public string User(Guid accountId, Guid userId) => WebAppRoutes.User(accountId, userId);

    public string Groups(Guid accountId) => WebAppRoutes.Groups(accountId);

    public string Group(Guid accountId, Guid groupId) => WebAppRoutes.Group(accountId, groupId);

    public string Roles(Guid accountId) => WebAppRoutes.Roles(accountId);

    public string Role(Guid accountId, Guid roleId) => WebAppRoutes.Role(accountId, roleId);

    public string Permissions(Guid accountId) => WebAppRoutes.Permissions(accountId);

    public string Permission(Guid accountId, Guid permissionId) =>
        WebAppRoutes.Permission(accountId, permissionId);

    public string Audit(Guid accountId) => WebAppRoutes.Audit(accountId);

    public string AuditEntry(Guid accountId, Guid entryId) =>
        WebAppRoutes.AuditEntry(accountId, entryId);
}
