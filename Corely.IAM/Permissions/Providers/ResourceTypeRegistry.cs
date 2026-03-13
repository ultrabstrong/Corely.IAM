using System.Collections.Concurrent;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Models;

namespace Corely.IAM.Permissions.Providers;

internal class ResourceTypeRegistry : IResourceTypeRegistry
{
    private readonly ConcurrentDictionary<string, ResourceTypeInfo> _resourceTypes = new(
        StringComparer.OrdinalIgnoreCase
    );

    public ResourceTypeRegistry()
    {
        _resourceTypes[PermissionConstants.ACCOUNT_RESOURCE_TYPE] = new ResourceTypeInfo(
            PermissionConstants.ACCOUNT_RESOURCE_TYPE,
            "Accounts"
        );
        _resourceTypes[PermissionConstants.USER_RESOURCE_TYPE] = new ResourceTypeInfo(
            PermissionConstants.USER_RESOURCE_TYPE,
            "Users"
        );
        _resourceTypes[PermissionConstants.GROUP_RESOURCE_TYPE] = new ResourceTypeInfo(
            PermissionConstants.GROUP_RESOURCE_TYPE,
            "Groups"
        );
        _resourceTypes[PermissionConstants.ROLE_RESOURCE_TYPE] = new ResourceTypeInfo(
            PermissionConstants.ROLE_RESOURCE_TYPE,
            "Roles"
        );
        _resourceTypes[PermissionConstants.PERMISSION_RESOURCE_TYPE] = new ResourceTypeInfo(
            PermissionConstants.PERMISSION_RESOURCE_TYPE,
            "Permissions"
        );
        _resourceTypes[PermissionConstants.ALL_RESOURCE_TYPES] = new ResourceTypeInfo(
            PermissionConstants.ALL_RESOURCE_TYPES,
            "All resource types (wildcard)"
        );
    }

    public IReadOnlyCollection<ResourceTypeInfo> GetAll() =>
        _resourceTypes.Values.ToList().AsReadOnly();

    public ResourceTypeInfo? Get(string name) => _resourceTypes.GetValueOrDefault(name);

    public bool Exists(string name) => _resourceTypes.ContainsKey(name);

    internal void Register(string name, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var info = new ResourceTypeInfo(name, description);
        if (!_resourceTypes.TryAdd(name, info))
        {
            throw new InvalidOperationException($"Resource type '{name}' is already registered.");
        }
    }
}
