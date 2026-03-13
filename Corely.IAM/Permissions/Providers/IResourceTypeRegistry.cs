using Corely.IAM.Permissions.Models;

namespace Corely.IAM.Permissions.Providers;

public interface IResourceTypeRegistry
{
    IReadOnlyCollection<ResourceTypeInfo> GetAll();
    ResourceTypeInfo? Get(string name);
    bool Exists(string name);
}
