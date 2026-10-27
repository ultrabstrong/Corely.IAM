using System.Collections.Concurrent;
using Corely.IAM.Accounts.Models;
using Corely.IAM.ApiKeys.Models;
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
        AddIamType(Account.RESOURCE_TYPE, "Accounts", "An account and its settings");
        AddIamType(User.RESOURCE_TYPE, "Users", "The account's users and what they belong to");
        AddIamType(Group.RESOURCE_TYPE, "Groups", "Groups of users and the roles they carry");
        AddIamType(Role.RESOURCE_TYPE, "Roles", "Roles and the permissions they hold");
        AddIamType(
            Permission.RESOURCE_TYPE,
            "Permissions",
            "What a permission allows, on which resources"
        );
        AddIamType(
            ApiKey.RESOURCE_TYPE,
            "API keys",
            "Keys a program signs in with, their secrets and roles"
        );
        AddIamType(
            AuditEntry.RESOURCE_TYPE,
            "Audit log",
            "The account's audit entries",
            AuthAction.Read,
            AuthAction.Delete
        );
        AddIamType(
            AccountAuditSettings.RESOURCE_TYPE,
            "Audit settings",
            "What the account audits and for how long",
            AuthAction.Read,
            AuthAction.Update
        );
        AddPlatformSettingsType(
            PlatformSettings.RESOURCE_TYPE,
            "Audit",
            "Platform wide audit defaults"
        );
        AddPlatformSettingsType(
            SecurityPolicy.RESOURCE_TYPE,
            "Security",
            "Passwords, sign in, two factor and API keys for every account"
        );
    }

    public IReadOnlyCollection<ResourceTypeInfo> GetAll() =>
        _resourceTypes.Values.ToList().AsReadOnly();

    public ResourceTypeInfo? Get(string name) => _resourceTypes.GetValueOrDefault(name);

    public bool Exists(string name) => _resourceTypes.ContainsKey(name);

    internal void Register(string name, string description, IEnumerable<AuthAction> ownerActions) =>
        Register(
            new ResourceTypeInfo(
                name,
                description,
                [.. ownerActions ?? throw new ArgumentNullException(nameof(ownerActions))]
            )
        );

    internal void Register(ResourceTypeInfo type)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(type.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(type.Description);

        if (type.Name == "*")
        {
            throw new ArgumentException(
                "'*' is not a resource type. Permissions name the exact type they grant.",
                nameof(type)
            );
        }

        var info = type with { OwnerActions = [.. type.OwnerActions.Distinct()] };
        if (!_resourceTypes.TryAdd(type.Name, info))
        {
            throw new InvalidOperationException(
                $"Resource type '{type.Name}' is already registered."
            );
        }
    }

    private void AddIamType(string name, string title, string description) =>
        AddIamType(name, title, description, _allActions);

    private void AddIamType(
        string name,
        string title,
        string description,
        params AuthAction[] ownerActions
    ) =>
        _resourceTypes[name] = new ResourceTypeInfo(name, description, ownerActions)
        {
            Title = title,
            Group = ResourceTypeInfo.IDENTITY_AND_ACCESS_GROUP,
        };

    private void AddPlatformSettingsType(string name, string title, string description) =>
        _resourceTypes[name] = new ResourceTypeInfo(name, description, [])
        {
            Title = title,
            Group = ResourceTypeInfo.PLATFORM_SETTINGS_GROUP,
        };
}
