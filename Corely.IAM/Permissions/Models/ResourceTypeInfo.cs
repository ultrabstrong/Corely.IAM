using Corely.IAM.Security.Enums;

namespace Corely.IAM.Permissions.Models;

public record ResourceTypeInfo(
    string Name,
    string Description,
    IReadOnlyList<AuthAction> OwnerActions
)
{
    public const string IDENTITY_AND_ACCESS_GROUP = "Identity and access";
    public const string PLATFORM_SETTINGS_GROUP = "Platform settings";

    public string Title { get; init; } = Name;
    public string? Group { get; init; }

    public string OwnerDefaultDescription() =>
        PermissionLabelProvider.GetName(
            Name,
            Guid.Empty,
            OwnerActions.Contains(AuthAction.Create),
            OwnerActions.Contains(AuthAction.Read),
            OwnerActions.Contains(AuthAction.Update),
            OwnerActions.Contains(AuthAction.Delete),
            OwnerActions.Contains(AuthAction.Execute)
        );
}
