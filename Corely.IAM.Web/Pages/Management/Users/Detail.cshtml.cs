using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Microsoft.AspNetCore.Mvc;

namespace Corely.IAM.Web.Pages.Management.Users;

public class DetailModel(
    IRetrievalService retrievalService,
    IModificationService modificationService,
    IRegistrationService registrationService,
    IDeregistrationService deregistrationService
) : DetailPageModelBase<User>
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<ChildRef> Roles { get; set; } = [];

    protected override string IndexPagePath => "/Management/Users/Index";

    public async Task<IActionResult> OnPostEditAsync(Guid id, string username, string email)
    {
        var result = await modificationService.ModifyUserAsync(
            new UpdateUserRequest(id, username, email)
        );

        SetResultMessage(
            result.ResultCode == ModifyResultCode.Success,
            "User updated successfully.",
            result.Message
        );

        return await ReloadAsync(id);
    }

    public async Task<IActionResult> OnPostAddRoleAsync(Guid id, Guid roleId)
    {
        var result = await registrationService.RegisterRolesWithUserAsync(
            new RegisterRolesWithUserRequest([roleId], id)
        );

        SetResultMessage(
            result.ResultCode == AssignRolesToUserResultCode.Success,
            "Role assigned successfully.",
            result.Message ?? "Failed to assign role."
        );

        return await ReloadAsync(id);
    }

    public async Task<IActionResult> OnPostRemoveRoleAsync(Guid id, Guid roleId)
    {
        var result = await deregistrationService.DeregisterRolesFromUserAsync(
            new DeregisterRolesFromUserRequest([roleId], id)
        );

        SetResultMessage(
            result.ResultCode == DeregisterRolesFromUserResultCode.Success,
            "Role removed successfully.",
            result.Message ?? "Failed to remove role."
        );

        return await ReloadAsync(id);
    }

    protected override async Task<User?> LoadItemAsync(Guid id)
    {
        var result = await retrievalService.GetUserAsync(id, hydrate: true);
        return result.ResultCode == RetrieveResultCode.Success ? result.Item : null;
    }

    protected override void PopulateFromItem(User item)
    {
        Id = item.Id;
        Username = item.Username;
        Email = item.Email;
        Roles = item.Roles ?? [];
    }
}
