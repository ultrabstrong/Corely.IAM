using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;
using Corely.IAM.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Authorization;

public class GrantRevocationTests : IAsyncLifetime
{
    private readonly IamScenario _scenario = new();

    public Task InitializeAsync() => _scenario.InitializeAsync();

    public Task DisposeAsync() => _scenario.DisposeAsync();

    [Fact]
    public async Task RemovingARoleFromAUser_RevokesTheDerivedAccess()
    {
        Assert.True(await DirectMemberCanReadUsersAsync(), "Precondition: access is granted.");

        var removal = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IDeregistrationService>()
                    .DeregisterRolesFromUserAsync(
                        new DeregisterRolesFromUserRequest(
                            [_scenario.ReaderRoleId],
                            _scenario.DirectMemberUserId,
                            _scenario.AccountId
                        )
                    )
        );
        Assert.Equal(DeregisterRolesFromUserResultCode.Success, removal.ResultCode);

        Assert.False(
            await DirectMemberCanReadUsersAsync(),
            "Access must not survive removal of the role that granted it."
        );
    }

    [Fact]
    public async Task RemovingAUserFromAGroup_RevokesTheGroupDerivedAccess()
    {
        Assert.True(await GroupMemberCanReadEditorRoleAsync(), "Precondition: access is granted.");

        var removal = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IDeregistrationService>()
                    .DeregisterUsersFromGroupAsync(
                        new DeregisterUsersFromGroupRequest(
                            [_scenario.GroupMemberUserId],
                            _scenario.EditorGroupId,
                            _scenario.AccountId
                        )
                    )
        );
        Assert.Equal(DeregisterUsersFromGroupResultCode.Success, removal.ResultCode);

        Assert.False(
            await GroupMemberCanReadEditorRoleAsync(),
            "Access must not survive removal from the group that granted it."
        );
    }

    [Fact]
    public async Task RemovingARoleFromAGroup_RevokesTheDerivedAccess()
    {
        Assert.True(await GroupMemberCanReadEditorRoleAsync(), "Precondition: access is granted.");

        var removal = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IDeregistrationService>()
                    .DeregisterRolesFromGroupAsync(
                        new DeregisterRolesFromGroupRequest(
                            [_scenario.EditorRoleId],
                            _scenario.EditorGroupId,
                            _scenario.AccountId
                        )
                    )
        );
        Assert.Equal(DeregisterRolesFromGroupResultCode.Success, removal.ResultCode);

        Assert.False(await GroupMemberCanReadEditorRoleAsync());
    }

    [Fact]
    public async Task RemovingAUserFromAnAccount_RevokesEverythingInIt()
    {
        Assert.True(await DirectMemberCanReadUsersAsync(), "Precondition: access is granted.");

        var removal = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IDeregistrationService>()
                    .DeregisterUserFromAccountAsync(
                        new DeregisterUserFromAccountRequest(
                            _scenario.DirectMemberUserId,
                            _scenario.AccountId
                        )
                    )
        );
        Assert.Equal(DeregisterUserFromAccountResultCode.Success, removal.ResultCode);

        var authorized = await _scenario.IsAuthorizedAsync(
            _scenario.DirectMemberUsername,
            null,
            AuthAction.Read,
            PermissionConstants.USER_RESOURCE_TYPE,
            _scenario.OwnerUserId
        );
        Assert.False(authorized);
    }

    private Task<bool> DirectMemberCanReadUsersAsync() =>
        _scenario.IsAuthorizedAsync(
            _scenario.DirectMemberUsername,
            _scenario.AccountId,
            AuthAction.Read,
            PermissionConstants.USER_RESOURCE_TYPE,
            _scenario.OwnerUserId
        );

    private Task<bool> GroupMemberCanReadEditorRoleAsync() =>
        _scenario.IsAuthorizedAsync(
            _scenario.GroupMemberUsername,
            _scenario.AccountId,
            AuthAction.Read,
            PermissionConstants.ROLE_RESOURCE_TYPE,
            _scenario.EditorRoleId
        );
}
