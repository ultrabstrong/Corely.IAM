using Corely.IAM.Security.Constants;

namespace Corely.IAM.Permissions.Models;

public record ResourceTypeInfo(
    string Name,
    string Description,
    IReadOnlyList<AuthAction> OwnerActions
);
