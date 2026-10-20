namespace Corely.IAM.Web.UnitTests.Helpers;

public class FakeIamWebLinks : IIamWebLinks
{
    public const string PREFIX = "/fake";

    public string Dashboard() => $"{PREFIX}/dashboard";

    public string Profile() => $"{PREFIX}/profile";

    public string AcceptInvitation() => $"{PREFIX}/accept-invitation";

    public string Account(Guid accountId) => $"{PREFIX}/account/{accountId}";

    public string Users(Guid accountId) => $"{PREFIX}/users/{accountId}";

    public string User(Guid accountId, Guid userId) => $"{PREFIX}/user/{accountId}/{userId}";

    public string Groups(Guid accountId) => $"{PREFIX}/groups/{accountId}";

    public string Group(Guid accountId, Guid groupId) => $"{PREFIX}/group/{accountId}/{groupId}";

    public string Roles(Guid accountId) => $"{PREFIX}/roles/{accountId}";

    public string Role(Guid accountId, Guid roleId) => $"{PREFIX}/role/{accountId}/{roleId}";

    public string Permissions(Guid accountId) => $"{PREFIX}/permissions/{accountId}";

    public string Permission(Guid accountId, Guid permissionId) =>
        $"{PREFIX}/permission/{accountId}/{permissionId}";

    public string Audit(Guid accountId) => $"{PREFIX}/audit/{accountId}";

    public string AuditEntry(Guid accountId, Guid entryId) =>
        $"{PREFIX}/audit-entry/{accountId}/{entryId}";
}
