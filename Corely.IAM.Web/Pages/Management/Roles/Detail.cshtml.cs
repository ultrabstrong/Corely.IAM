using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Services;
using Microsoft.AspNetCore.Mvc;

namespace Corely.IAM.Web.Pages.Management.Roles;

public class DetailModel(
    IRetrievalService retrievalService,
    IModificationService modificationService,
    IRegistrationService registrationService,
    IDeregistrationService deregistrationService
) : DetailPageModelBase<Role>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemDefined { get; set; }
    public List<ChildRef> Permissions { get; set; } = [];

    protected override string IndexPagePath => "/Management/Roles/Index";

    protected override async Task<Role?> LoadItemAsync(Guid id)
    {
        var result = await retrievalService.GetRoleAsync(id, hydrate: true);
        return result.ResultCode == RetrieveResultCode.Success ? result.Item : null;
    }

    protected override void PopulateFromItem(Role item)
    {
        Id = item.Id;
        Name = item.Name;
        Description = item.Description;
        IsSystemDefined = item.IsSystemDefined;
        Permissions = item.Permissions ?? [];
    }

    public async Task<IActionResult> OnPostEditAsync(Guid id, string name, string? description)
    {
        var result = await modificationService.ModifyRoleAsync(
            new UpdateRoleRequest(id, name, description)
        );

        SetResultMessage(
            result.ResultCode == ModifyResultCode.Success,
            "Role updated successfully.",
            result.Message
        );

        return await ReloadAsync(id);
    }

    public async Task<IActionResult> OnPostAddPermissionAsync(Guid id, Guid permissionId)
    {
        var result = await registrationService.RegisterPermissionsWithRoleAsync(
            new RegisterPermissionsWithRoleRequest([permissionId], id)
        );

        SetResultMessage(
            result.ResultCode == AssignPermissionsToRoleResultCode.Success,
            "Permission added successfully.",
            result.Message ?? "Failed to add permission."
        );

        return await ReloadAsync(id);
    }

    public async Task<IActionResult> OnPostRemovePermissionAsync(Guid id, Guid permissionId)
    {
        var result = await deregistrationService.DeregisterPermissionsFromRoleAsync(
            new DeregisterPermissionsFromRoleRequest([permissionId], id)
        );

        SetResultMessage(
            result.ResultCode == DeregisterPermissionsFromRoleResultCode.Success,
            "Permission removed successfully.",
            result.Message ?? "Failed to remove permission."
        );

        return await ReloadAsync(id);
    }
}
