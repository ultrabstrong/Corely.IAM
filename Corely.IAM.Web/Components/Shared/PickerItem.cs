namespace Corely.IAM.Web.Components.Shared;

public record PickerItem(
    Guid Id,
    string DisplayText,
    string? Description = null,
    string? Group = null,
    int GroupOrder = 0
);
