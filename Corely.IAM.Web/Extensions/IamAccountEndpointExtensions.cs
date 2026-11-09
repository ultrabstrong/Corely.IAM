using Corely.IAM.Audits.Models;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Corely.IAM.Web.Extensions;

public static class IamAccountEndpointExtensions
{
    public const string ACCOUNT_ENDPOINTS_TAG = "IAM account";
    public const int DEFAULT_TAKE = 25;
    public const int MAX_TAKE = 100;

    extension(IEndpointRouteBuilder endpoints)
    {
        public RouteGroupBuilder MapIAMAccountEndpoints(string prefix = "/api")
        {
            var group = endpoints
                .MapGroup(prefix)
                .WithTags(ACCOUNT_ENDPOINTS_TAG)
                .AsIAMApi()
                .RequireAuthorization();

            group
                .MapGet("/users", ListUsersAsync)
                .WithName("IamListUsers")
                .Produces<ApiResponse<PageResponse<UserResponse>>>()
                .WithSummary("The account's members");
            group
                .MapDelete("/users/{userId:guid}", RemoveUserAsync)
                .WithName("IamRemoveUser")
                .Produces<ApiResponse<object>>()
                .WithSummary("Remove a member from the account");
            group
                .MapPost("/users/{userId:guid}/roles", AddRolesToUserAsync)
                .WithName("IamAddRolesToUser")
                .Produces<ApiResponse<object>>()
                .WithSummary("Give a member roles");
            group
                .MapDelete("/users/{userId:guid}/roles/{roleId:guid}", RemoveRoleFromUserAsync)
                .WithName("IamRemoveRoleFromUser")
                .Produces<ApiResponse<object>>()
                .WithSummary("Take a role from a member");

            group
                .MapGet("/invitations", ListInvitationsAsync)
                .WithName("IamListInvitations")
                .Produces<ApiResponse<PageResponse<InvitationResponse>>>()
                .WithSummary("The account's invitations");
            group
                .MapPost("/invitations", CreateInvitationAsync)
                .WithName("IamCreateInvitation")
                .Produces<ApiResponse<CreatedInvitationResponse>>()
                .WithSummary(
                    "Invite someone by email. The token is returned once; send it to them"
                );
            group
                .MapDelete("/invitations/{invitationId:guid}", RevokeInvitationAsync)
                .WithName("IamRevokeInvitation")
                .Produces<ApiResponse<object>>()
                .WithSummary("Revoke an invitation");

            group
                .MapGet("/groups", ListGroupsAsync)
                .WithName("IamListGroups")
                .Produces<ApiResponse<PageResponse<GroupResponse>>>()
                .WithSummary("The account's groups");
            group
                .MapGet("/groups/{groupId:guid}", GetGroupAsync)
                .WithName("IamGetGroup")
                .Produces<ApiResponse<GroupResponse>>()
                .WithSummary("A group with its members, keys and roles");
            group
                .MapPost("/groups", CreateGroupAsync)
                .WithName("IamCreateGroup")
                .Produces<ApiResponse<CreatedResponse>>()
                .WithSummary("Create a group");
            group
                .MapPut("/groups/{groupId:guid}", UpdateGroupAsync)
                .WithName("IamUpdateGroup")
                .Produces<ApiResponse<object>>()
                .WithSummary("Rename or describe a group");
            group
                .MapDelete("/groups/{groupId:guid}", DeleteGroupAsync)
                .WithName("IamDeleteGroup")
                .Produces<ApiResponse<object>>()
                .WithSummary("Delete a group");
            group
                .MapPost("/groups/{groupId:guid}/users", AddUsersToGroupAsync)
                .WithName("IamAddUsersToGroup")
                .Produces<ApiResponse<object>>()
                .WithSummary("Add members to a group");
            group
                .MapDelete("/groups/{groupId:guid}/users/{userId:guid}", RemoveUserFromGroupAsync)
                .WithName("IamRemoveUserFromGroup")
                .Produces<ApiResponse<object>>()
                .WithSummary("Take a member out of a group");
            group
                .MapPost("/groups/{groupId:guid}/apikeys", AddApiKeysToGroupAsync)
                .WithName("IamAddApiKeysToGroup")
                .Produces<ApiResponse<object>>()
                .WithSummary("Add API keys to a group");
            group
                .MapDelete(
                    "/groups/{groupId:guid}/apikeys/{apiKeyId:guid}",
                    RemoveApiKeyFromGroupAsync
                )
                .WithName("IamRemoveApiKeyFromGroup")
                .Produces<ApiResponse<object>>()
                .WithSummary("Take an API key out of a group");
            group
                .MapPost("/groups/{groupId:guid}/roles", AddRolesToGroupAsync)
                .WithName("IamAddRolesToGroup")
                .Produces<ApiResponse<object>>()
                .WithSummary("Give a group roles");
            group
                .MapDelete("/groups/{groupId:guid}/roles/{roleId:guid}", RemoveRoleFromGroupAsync)
                .WithName("IamRemoveRoleFromGroup")
                .Produces<ApiResponse<object>>()
                .WithSummary("Take a role from a group");

            group
                .MapGet("/roles", ListRolesAsync)
                .WithName("IamListRoles")
                .Produces<ApiResponse<PageResponse<RoleResponse>>>()
                .WithSummary("The account's roles");
            group
                .MapGet("/roles/{roleId:guid}", GetRoleAsync)
                .WithName("IamGetRole")
                .Produces<ApiResponse<RoleResponse>>()
                .WithSummary("A role with who holds it and its permissions");
            group
                .MapPost("/roles", CreateRoleAsync)
                .WithName("IamCreateRole")
                .Produces<ApiResponse<CreatedResponse>>()
                .WithSummary("Create a role");
            group
                .MapPut("/roles/{roleId:guid}", UpdateRoleAsync)
                .WithName("IamUpdateRole")
                .Produces<ApiResponse<object>>()
                .WithSummary("Rename or describe a role");
            group
                .MapDelete("/roles/{roleId:guid}", DeleteRoleAsync)
                .WithName("IamDeleteRole")
                .Produces<ApiResponse<object>>()
                .WithSummary("Delete a role");
            group
                .MapPost("/roles/{roleId:guid}/permissions", AddPermissionsToRoleAsync)
                .WithName("IamAddPermissionsToRole")
                .Produces<ApiResponse<object>>()
                .WithSummary("Add permissions to a role");
            group
                .MapDelete(
                    "/roles/{roleId:guid}/permissions/{permissionId:guid}",
                    RemovePermissionFromRoleAsync
                )
                .WithName("IamRemovePermissionFromRole")
                .Produces<ApiResponse<object>>()
                .WithSummary("Take a permission from a role");

            group
                .MapGet("/permissions", ListPermissionsAsync)
                .WithName("IamListPermissions")
                .Produces<ApiResponse<PageResponse<PermissionResponse>>>()
                .WithSummary("The account's permissions");
            group
                .MapGet("/permissions/{permissionId:guid}", GetPermissionAsync)
                .WithName("IamGetPermission")
                .Produces<ApiResponse<PermissionResponse>>()
                .WithSummary("A permission and the roles that hold it");
            group
                .MapPost("/permissions", CreatePermissionAsync)
                .WithName("IamCreatePermission")
                .Produces<ApiResponse<CreatedResponse>>()
                .WithSummary(
                    "Create a permission on a resource type, or on one resource by its id"
                );
            group
                .MapPut("/permissions/{permissionId:guid}", UpdatePermissionAsync)
                .WithName("IamUpdatePermission")
                .Produces<ApiResponse<object>>()
                .WithSummary("Describe a permission");
            group
                .MapDelete("/permissions/{permissionId:guid}", DeletePermissionAsync)
                .WithName("IamDeletePermission")
                .Produces<ApiResponse<object>>()
                .WithSummary("Delete a permission");

            group
                .MapGet("/apikeys", ListApiKeysAsync)
                .WithName("IamListApiKeys")
                .Produces<ApiResponse<PageResponse<ApiKeyResponse>>>()
                .WithSummary("The account's API keys, never their secrets");
            group
                .MapGet("/apikeys/{apiKeyId:guid}", GetApiKeyAsync)
                .WithName("IamGetApiKey")
                .Produces<ApiResponse<ApiKeyResponse>>()
                .WithSummary("An API key, never its secret");
            group
                .MapPost("/apikeys", CreateApiKeyAsync)
                .WithName("IamCreateApiKey")
                .Produces<ApiResponse<CreatedApiKeyResponse>>()
                .WithSummary(
                    "Create an API key. Its key is returned once and can never be read again"
                );
            group
                .MapPut("/apikeys/{apiKeyId:guid}", UpdateApiKeyAsync)
                .WithName("IamUpdateApiKey")
                .Produces<ApiResponse<object>>()
                .WithSummary("Rename or describe an API key");
            group
                .MapDelete("/apikeys/{apiKeyId:guid}", DeleteApiKeyAsync)
                .WithName("IamDeleteApiKey")
                .Produces<ApiResponse<object>>()
                .WithSummary("Revoke an API key");
            group
                .MapPost("/apikeys/{apiKeyId:guid}/roles", AddRolesToApiKeyAsync)
                .WithName("IamAddRolesToApiKey")
                .Produces<ApiResponse<object>>()
                .WithSummary("Give an API key roles");
            group
                .MapDelete(
                    "/apikeys/{apiKeyId:guid}/roles/{roleId:guid}",
                    RemoveRoleFromApiKeyAsync
                )
                .WithName("IamRemoveRoleFromApiKey")
                .Produces<ApiResponse<object>>()
                .WithSummary("Take a role from an API key");

            group
                .MapGet("/audit", ListAuditEntriesAsync)
                .WithName("IamListAuditEntries")
                .Produces<ApiResponse<PageResponse<AuditEntryResponse>>>()
                .WithSummary("The account's audit log, newest first");

            return group;
        }
    }

    private static Guid? AccountOf(IUserContextProvider context) =>
        context.GetUserContext()?.CurrentAccount?.Id;

    private static IResult NoAccount() =>
        TypedResults.Json(
            ApiResponse.Failure("Unauthorized", "Sign in to an account first."),
            statusCode: StatusCodes.Status401Unauthorized
        );

    private static IResult BadPage() =>
        TypedResults.Json(
            ApiResponse.Failure(
                "ValidationError",
                $"skip must be zero or more and take between 1 and {MAX_TAKE}"
            ),
            statusCode: StatusCodes.Status400BadRequest
        );

    private static bool IsPage(int skip, int take) => skip >= 0 && take is >= 1 and <= MAX_TAKE;

    private static IResult Page<TModel, T>(
        RetrieveListResult<TModel> result,
        Func<TModel, T> map
    ) =>
        result.ResultCode.ToApiResult(
            result.Message,
            result.Data is { } page ? PageResponse<T>.From(page, map) : null
        );

    private static IResult Single<TModel, T>(
        RetrieveSingleResult<TModel> result,
        Func<TModel, T> map
    )
        where TModel : class =>
        result.ResultCode.ToApiResult(result.Message, result.Item is { } item ? map(item) : null);

    private static async Task<IResult> ListUsersAsync(
        IUserContextProvider context,
        IRetrievalService retrieval,
        int skip = 0,
        int take = DEFAULT_TAKE
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        if (!IsPage(skip, take))
            return BadPage();
        return Page(
            await retrieval.ListUsersAsync(new(accountId, Skip: skip, Take: take)),
            UserResponse.From
        );
    }

    private static async Task<IResult> RemoveUserAsync(
        Guid userId,
        IUserContextProvider context,
        IDeregistrationService deregistration
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await deregistration.DeregisterUserFromAccountAsync(new(userId, accountId));
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> AddRolesToUserAsync(
        Guid userId,
        IdsBody body,
        IUserContextProvider context,
        IRegistrationService registration
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await registration.RegisterRolesWithUserAsync(
            new(body.Ids ?? [], userId, accountId)
        );
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> RemoveRoleFromUserAsync(
        Guid userId,
        Guid roleId,
        IUserContextProvider context,
        IDeregistrationService deregistration
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await deregistration.DeregisterRolesFromUserAsync(
            new([roleId], userId, accountId)
        );
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> ListInvitationsAsync(
        IUserContextProvider context,
        IInvitationService invitations,
        int skip = 0,
        int take = DEFAULT_TAKE
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        if (!IsPage(skip, take))
            return BadPage();
        return Page(
            await invitations.ListInvitationsAsync(new(accountId, Skip: skip, Take: take)),
            InvitationResponse.From
        );
    }

    private static async Task<IResult> CreateInvitationAsync(
        InvitationBody body,
        IUserContextProvider context,
        IInvitationService invitations
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await invitations.CreateInvitationAsync(
            new(
                accountId,
                body.Email,
                body.Description,
                body.ExpiresInSeconds ?? CreateInvitationRequest.DEFAULT_EXPIRY_SECONDS
            )
        );
        return result.ResultCode.ToApiResult(
            result.Message,
            result is { InvitationId: { } id, Token: { } token }
                ? new CreatedInvitationResponse(id, token)
                : null
        );
    }

    private static async Task<IResult> RevokeInvitationAsync(
        Guid invitationId,
        IUserContextProvider context,
        IInvitationService invitations
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await invitations.RevokeInvitationAsync(new(accountId, invitationId));
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> ListGroupsAsync(
        IUserContextProvider context,
        IRetrievalService retrieval,
        int skip = 0,
        int take = DEFAULT_TAKE
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        if (!IsPage(skip, take))
            return BadPage();
        return Page(
            await retrieval.ListGroupsAsync(new(accountId, Skip: skip, Take: take)),
            GroupResponse.From
        );
    }

    private static async Task<IResult> GetGroupAsync(Guid groupId, IRetrievalService retrieval) =>
        Single(await retrieval.GetGroupAsync(groupId, hydrate: true), GroupResponse.From);

    private static async Task<IResult> CreateGroupAsync(
        NameBody body,
        IUserContextProvider context,
        IRegistrationService registration,
        IModificationService modification
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await registration.RegisterGroupAsync(new(body.Name, accountId));
        if (result.ResultCode.ApiStatusCode() == StatusCodes.Status200OK && body.Description is { })
            await modification.ModifyGroupAsync(
                new(result.CreatedGroupId, accountId, body.Name, body.Description)
            );
        return result.ResultCode.ToApiResult(
            result.Message,
            new CreatedResponse(result.CreatedGroupId)
        );
    }

    private static async Task<IResult> UpdateGroupAsync(
        Guid groupId,
        NameBody body,
        IUserContextProvider context,
        IModificationService modification
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await modification.ModifyGroupAsync(
            new(groupId, accountId, body.Name, body.Description)
        );
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> DeleteGroupAsync(
        Guid groupId,
        IUserContextProvider context,
        IDeregistrationService deregistration
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await deregistration.DeregisterGroupAsync(new(groupId, accountId));
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> AddUsersToGroupAsync(
        Guid groupId,
        IdsBody body,
        IUserContextProvider context,
        IRegistrationService registration
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await registration.RegisterUsersWithGroupAsync(
            new(body.Ids ?? [], groupId, accountId)
        );
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> RemoveUserFromGroupAsync(
        Guid groupId,
        Guid userId,
        IUserContextProvider context,
        IDeregistrationService deregistration
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await deregistration.DeregisterUsersFromGroupAsync(
            new([userId], groupId, accountId)
        );
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> AddApiKeysToGroupAsync(
        Guid groupId,
        IdsBody body,
        IUserContextProvider context,
        IRegistrationService registration
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await registration.RegisterApiKeysWithGroupAsync(
            new(body.Ids ?? [], groupId, accountId)
        );
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> RemoveApiKeyFromGroupAsync(
        Guid groupId,
        Guid apiKeyId,
        IUserContextProvider context,
        IDeregistrationService deregistration
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await deregistration.DeregisterApiKeysFromGroupAsync(
            new([apiKeyId], groupId, accountId)
        );
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> AddRolesToGroupAsync(
        Guid groupId,
        IdsBody body,
        IUserContextProvider context,
        IRegistrationService registration
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await registration.RegisterRolesWithGroupAsync(
            new(body.Ids ?? [], groupId, accountId)
        );
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> RemoveRoleFromGroupAsync(
        Guid groupId,
        Guid roleId,
        IUserContextProvider context,
        IDeregistrationService deregistration
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await deregistration.DeregisterRolesFromGroupAsync(
            new([roleId], groupId, accountId)
        );
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> ListRolesAsync(
        IUserContextProvider context,
        IRetrievalService retrieval,
        int skip = 0,
        int take = DEFAULT_TAKE
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        if (!IsPage(skip, take))
            return BadPage();
        return Page(
            await retrieval.ListRolesAsync(new(accountId, Skip: skip, Take: take)),
            RoleResponse.From
        );
    }

    private static async Task<IResult> GetRoleAsync(Guid roleId, IRetrievalService retrieval) =>
        Single(await retrieval.GetRoleAsync(roleId, hydrate: true), RoleResponse.From);

    private static async Task<IResult> CreateRoleAsync(
        NameBody body,
        IUserContextProvider context,
        IRegistrationService registration,
        IModificationService modification
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await registration.RegisterRoleAsync(new(body.Name, accountId));
        if (result.ResultCode.ApiStatusCode() == StatusCodes.Status200OK && body.Description is { })
            await modification.ModifyRoleAsync(
                new(result.CreatedRoleId, accountId, body.Name, body.Description)
            );
        return result.ResultCode.ToApiResult(
            result.Message,
            new CreatedResponse(result.CreatedRoleId)
        );
    }

    private static async Task<IResult> UpdateRoleAsync(
        Guid roleId,
        NameBody body,
        IUserContextProvider context,
        IModificationService modification
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await modification.ModifyRoleAsync(
            new(roleId, accountId, body.Name, body.Description)
        );
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> DeleteRoleAsync(
        Guid roleId,
        IUserContextProvider context,
        IDeregistrationService deregistration
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await deregistration.DeregisterRoleAsync(new(roleId, accountId));
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> AddPermissionsToRoleAsync(
        Guid roleId,
        IdsBody body,
        IUserContextProvider context,
        IRegistrationService registration
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await registration.RegisterPermissionsWithRoleAsync(
            new(body.Ids ?? [], roleId, accountId)
        );
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> RemovePermissionFromRoleAsync(
        Guid roleId,
        Guid permissionId,
        IUserContextProvider context,
        IDeregistrationService deregistration
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await deregistration.DeregisterPermissionsFromRoleAsync(
            new([permissionId], roleId, accountId)
        );
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> ListPermissionsAsync(
        IUserContextProvider context,
        IRetrievalService retrieval,
        int skip = 0,
        int take = DEFAULT_TAKE
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        if (!IsPage(skip, take))
            return BadPage();
        return Page(
            await retrieval.ListPermissionsAsync(new(accountId, Skip: skip, Take: take)),
            PermissionResponse.From
        );
    }

    private static async Task<IResult> GetPermissionAsync(
        Guid permissionId,
        IRetrievalService retrieval
    ) =>
        Single(
            await retrieval.GetPermissionAsync(permissionId, hydrate: true),
            PermissionResponse.From
        );

    private static async Task<IResult> CreatePermissionAsync(
        PermissionBody body,
        IUserContextProvider context,
        IRegistrationService registration
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await registration.RegisterPermissionAsync(
            new(
                accountId,
                body.ResourceType,
                body.ResourceId ?? Guid.Empty,
                body.Create,
                body.Read,
                body.Update,
                body.Delete,
                body.Execute,
                body.Description
            )
        );
        return result.ResultCode.ToApiResult(
            result.Message,
            new CreatedResponse(result.CreatedPermissionId)
        );
    }

    private static async Task<IResult> UpdatePermissionAsync(
        Guid permissionId,
        DescriptionBody body,
        IUserContextProvider context,
        IModificationService modification
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await modification.ModifyPermissionAsync(
            new(permissionId, accountId, body.Description)
        );
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> DeletePermissionAsync(
        Guid permissionId,
        IUserContextProvider context,
        IDeregistrationService deregistration
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await deregistration.DeregisterPermissionAsync(new(permissionId, accountId));
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> ListApiKeysAsync(
        IUserContextProvider context,
        IRetrievalService retrieval,
        int skip = 0,
        int take = DEFAULT_TAKE
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        if (!IsPage(skip, take))
            return BadPage();
        return Page(
            await retrieval.ListApiKeysAsync(new(accountId, Skip: skip, Take: take)),
            ApiKeyResponse.From
        );
    }

    private static async Task<IResult> GetApiKeyAsync(Guid apiKeyId, IRetrievalService retrieval) =>
        Single(await retrieval.GetApiKeyAsync(apiKeyId, hydrate: true), ApiKeyResponse.From);

    private static async Task<IResult> CreateApiKeyAsync(
        ApiKeyBody body,
        IUserContextProvider context,
        IRegistrationService registration
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await registration.RegisterApiKeyAsync(
            new(body.Name, accountId, body.Description, body.ExpiresUtc)
        );
        return result.ResultCode.ToApiResult(
            result.Message,
            result.Key is { } key ? new CreatedApiKeyResponse(result.CreatedApiKeyId, key) : null
        );
    }

    private static async Task<IResult> UpdateApiKeyAsync(
        Guid apiKeyId,
        NameBody body,
        IUserContextProvider context,
        IModificationService modification
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await modification.ModifyApiKeyAsync(
            new(apiKeyId, accountId, body.Name, body.Description)
        );
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> DeleteApiKeyAsync(
        Guid apiKeyId,
        IUserContextProvider context,
        IDeregistrationService deregistration
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await deregistration.DeregisterApiKeyAsync(new(apiKeyId, accountId));
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> AddRolesToApiKeyAsync(
        Guid apiKeyId,
        IdsBody body,
        IUserContextProvider context,
        IRegistrationService registration
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await registration.RegisterRolesWithApiKeyAsync(
            new(body.Ids ?? [], apiKeyId, accountId)
        );
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> RemoveRoleFromApiKeyAsync(
        Guid apiKeyId,
        Guid roleId,
        IUserContextProvider context,
        IDeregistrationService deregistration
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        var result = await deregistration.DeregisterRolesFromApiKeyAsync(
            new([roleId], apiKeyId, accountId)
        );
        return result.ResultCode.ToApiResult(result.Message);
    }

    private static async Task<IResult> ListAuditEntriesAsync(
        IUserContextProvider context,
        IAuditService audit,
        DateTime? from = null,
        DateTime? to = null,
        string? resourceType = null,
        AuthAction? action = null,
        string? resultCode = null,
        int skip = 0,
        int take = DEFAULT_TAKE
    )
    {
        if (AccountOf(context) is not { } accountId)
            return NoAccount();
        if (!IsPage(skip, take))
            return BadPage();
        return Page(
            await audit.ListEntriesAsync(
                new(
                    new AuditEntryFilter
                    {
                        AccountId = accountId,
                        FromUtc = from,
                        ToUtc = to,
                        ResourceType = resourceType,
                        Action = action,
                        ResultCode = resultCode,
                    },
                    skip,
                    take
                )
            ),
            AuditEntryResponse.From
        );
    }
}
