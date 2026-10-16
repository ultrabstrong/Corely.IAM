using System.Collections.Concurrent;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Audits.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Permissions.Providers;

internal class ResourceTypeRegistry : IResourceTypeRegistry
{
    private static readonly AuthAction[] _allActions = Enum.GetValues<AuthAction>();

    private readonly ConcurrentDictionary<string, ResourceTypeInfo> _resourceTypes = new(
        StringComparer.OrdinalIgnoreCase
    );

    public ResourceTypeRegistry()
    {
        AddIamType(Account.RESOURCE_TYPE, "Accounts");
        AddIamType(User.RESOURCE_TYPE, "Users");
        AddIamType(Group.RESOURCE_TYPE, "Groups");
        AddIamType(Role.RESOURCE_TYPE, "Roles");
        AddIamType(Permission.RESOURCE_TYPE, "Permissions");
        AddIamType(AuditEntry.RESOURCE_TYPE, "Audit log", AuthAction.Read, AuthAction.Delete);
        AddIamType(
            AccountAuditSettings.RESOURCE_TYPE,
            "Audit settings",
            AuthAction.Read,
            AuthAction.Update
        );
        AddIamType(PlatformSettings.RESOURCE_TYPE, "Platform settings", []);
    }

    public IReadOnlyCollection<ResourceTypeInfo> GetAll() =>
        _resourceTypes.Values.ToList().AsReadOnly();

    public ResourceTypeInfo? Get(string name) => _resourceTypes.GetValueOrDefault(name);

    public bool Exists(string name) => _resourceTypes.ContainsKey(name);

    internal void Register(string name, string description, IEnumerable<AuthAction> ownerActions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(ownerActions);

        if (name == "*")
        {
            throw new ArgumentException(
                "'*' is not a resource type. Permissions name the exact type they grant.",
                nameof(name)
            );
        }

        var info = new ResourceTypeInfo(name, description, [.. ownerActions.Distinct()]);
        if (!_resourceTypes.TryAdd(name, info))
        {
            throw new InvalidOperationException($"Resource type '{name}' is already registered.");
        }
    }

    private void AddIamType(string name, string description) =>
        AddIamType(name, description, _allActions);

    private void AddIamType(string name, string description, params AuthAction[] ownerActions) =>
        _resourceTypes[name] = new ResourceTypeInfo(name, description, ownerActions);
}
