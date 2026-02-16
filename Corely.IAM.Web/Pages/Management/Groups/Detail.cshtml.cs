using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Microsoft.AspNetCore.Mvc;

namespace Corely.IAM.Web.Pages.Management.Groups;

public class DetailModel(
    IRetrievalService retrievalService,
    IModificationService modificationService,
    IRegistrationService registrationService,
    IDeregistrationService deregistrationService
) : DetailPageModelBase<Group>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<ChildRef> Users { get; set; } = [];
    public List<ChildRef> Roles { get; set; } = [];

    protected override string IndexPagePath => "/Management/Groups/Index";

    protected override async Task<Group?> LoadItemAsync(Guid id)
    {
        var result = await retrievalService.GetGroupAsync(id, hydrate: true);
        return result.ResultCode == RetrieveResultCode.Success ? result.Item : null;
    }

    protected override void PopulateFromItem(Group item)
    {
        Id = item.Id;
        Name = item.Name;
        Description = item.Description;
        Users = item.Users ?? [];
        Roles = item.Roles ?? [];
    }

    public async Task<IActionResult> OnPostEditAsync(Guid id, string name, string? description)
    {
        var result = await modificationService.ModifyGroupAsync(
            new UpdateGroupRequest(id, name, description)
        );

        SetResultMessage(
            result.ResultCode == ModifyResultCode.Success,
            "Group updated successfully.",
            result.Message
        );

        return await ReloadAsync(id);
    }

    public async Task<IActionResult> OnPostAddUserAsync(Guid id, Guid userId)
    {
        var result = await registrationService.RegisterUsersWithGroupAsync(
            new RegisterUsersWithGroupRequest([userId], id)
        );

        SetResultMessage(
            result.ResultCode == AddUsersToGroupResultCode.Success,
            "User added successfully.",
            result.Message ?? "Failed to add user."
        );

        return await ReloadAsync(id);
    }

    public async Task<IActionResult> OnPostRemoveUserAsync(Guid id, Guid userId)
    {
        var result = await deregistrationService.DeregisterUsersFromGroupAsync(
            new DeregisterUsersFromGroupRequest([userId], id)
        );

        SetResultMessage(
            result.ResultCode == DeregisterUsersFromGroupResultCode.Success,
            "User removed successfully.",
            result.Message ?? "Failed to remove user."
        );

        return await ReloadAsync(id);
    }

    public async Task<IActionResult> OnPostAddRoleAsync(Guid id, Guid roleId)
    {
        var result = await registrationService.RegisterRolesWithGroupAsync(
            new RegisterRolesWithGroupRequest([roleId], id)
        );

        SetResultMessage(
            result.ResultCode == AssignRolesToGroupResultCode.Success,
            "Role added successfully.",
            result.Message ?? "Failed to add role."
        );

        return await ReloadAsync(id);
    }

    public async Task<IActionResult> OnPostRemoveRoleAsync(Guid id, Guid roleId)
    {
        var result = await deregistrationService.DeregisterRolesFromGroupAsync(
            new DeregisterRolesFromGroupRequest([roleId], id)
        );

        SetResultMessage(
            result.ResultCode == DeregisterRolesFromGroupResultCode.Success,
            "Role removed successfully.",
            result.Message ?? "Failed to remove role."
        );

        return await ReloadAsync(id);
    }
}
