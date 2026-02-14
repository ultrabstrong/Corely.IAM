using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class ModificationService(
    IAccountProcessor accountProcessor,
    IUserProcessor userProcessor,
    IGroupProcessor groupProcessor,
    IRoleProcessor roleProcessor,
    ILogger<ModificationService> logger
) : IModificationService
{
    private readonly IAccountProcessor _accountProcessor = accountProcessor.ThrowIfNull(
        nameof(accountProcessor)
    );
    private readonly IUserProcessor _userProcessor = userProcessor.ThrowIfNull(
        nameof(userProcessor)
    );
    private readonly IGroupProcessor _groupProcessor = groupProcessor.ThrowIfNull(
        nameof(groupProcessor)
    );
    private readonly IRoleProcessor _roleProcessor = roleProcessor.ThrowIfNull(
        nameof(roleProcessor)
    );
    private readonly ILogger<ModificationService> _logger = logger.ThrowIfNull(nameof(logger));

    public async Task<ModifyResult> ModifyAccountAsync(UpdateAccountRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("Modifying account {AccountId}", request.AccountId);
        return await _accountProcessor.UpdateAccountAsync(request);
    }

    public async Task<ModifyResult> ModifyUserAsync(UpdateUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("Modifying user {UserId}", request.UserId);
        return await _userProcessor.UpdateUserAsync(request);
    }

    public async Task<ModifyResult> ModifyGroupAsync(UpdateGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("Modifying group {GroupId}", request.GroupId);
        return await _groupProcessor.UpdateGroupAsync(request);
    }

    public async Task<ModifyResult> ModifyRoleAsync(UpdateRoleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("Modifying role {RoleId}", request.RoleId);
        return await _roleProcessor.UpdateRoleAsync(request);
    }
}
