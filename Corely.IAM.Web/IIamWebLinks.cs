namespace Corely.IAM.Web;

public interface IIamWebLinks
{
    string Dashboard();
    string Profile();
    string AcceptInvitation();
    string Account(Guid accountId);
    string Users(Guid accountId);
    string User(Guid accountId, Guid userId);
    string Groups(Guid accountId);
    string Group(Guid accountId, Guid groupId);
    string Roles(Guid accountId);
    string Role(Guid accountId, Guid roleId);
    string Permissions(Guid accountId);
    string Permission(Guid accountId, Guid permissionId);
    string ApiKeys(Guid accountId);
    string ApiKey(Guid accountId, Guid apiKeyId);
    string Audit(Guid accountId);
    string AuditEntry(Guid accountId, Guid entryId);
}
