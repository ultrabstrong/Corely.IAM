using Corely.IAM.Models;
using Corely.IAM.Web.Extensions;
using Microsoft.AspNetCore.Http;

namespace Corely.IAM.Web.UnitTests.Extensions;

public class EnumExtensionsTests
{
    private static readonly HashSet<string> ConflictCodes =
    [
        "AccountExistsError",
        "ApiKeyExistsError",
        "GroupExistsError",
        "PermissionExistsError",
        "RoleExistsError",
        "UserExistsError",
        "UsernameExistsError",
        "EmailExistsError",
        "UserAlreadyInAccountError",
        "GroupHasSoleOwnersError",
        "UserIsSoleOwnerError",
        "UserIsSoleAccountOwnerError",
        "OwnerRoleRemovalBlockedError",
        "SystemDefinedError",
        "SystemDefinedPermissionError",
        "SystemDefinedRoleError",
        "SystemPermissionRemovalError",
        "SystemRoleAssignmentError",
        "ApiKeysDisabledError",
        "InvitationAlreadyAcceptedError",
        "InvitationExpiredError",
        "InvitationRevokedError",
    ];

    public static TheoryData<Enum> EveryAccountEndpointResultCode() =>
        [
            .. new[]
            {
                "RetrieveResultCode",
                "ModifyResultCode",
                "CreateGroupResultCode",
                "CreateRoleResultCode",
                "CreatePermissionResultCode",
                "CreateApiKeyResultCode",
                "AddUsersToGroupResultCode",
                "AssignRolesToGroupResultCode",
                "AssignRolesToUserResultCode",
                "AssignRolesToApiKeyResultCode",
                "AddApiKeysToGroupResultCode",
                "AssignPermissionsToRoleResultCode",
                "DeregisterGroupResultCode",
                "DeregisterRoleResultCode",
                "DeregisterApiKeyResultCode",
                "DeregisterPermissionResultCode",
                "DeregisterUserFromAccountResultCode",
                "DeregisterUsersFromGroupResultCode",
                "DeregisterRolesFromGroupResultCode",
                "DeregisterRolesFromUserResultCode",
                "DeregisterRolesFromApiKeyResultCode",
                "DeregisterApiKeysFromGroupResultCode",
                "DeregisterPermissionsFromRoleResultCode",
                "CreateInvitationResultCode",
                "RevokeInvitationResultCode",
            }
                .Select(name =>
                    typeof(RetrieveResultCode)
                        .Assembly.GetTypes()
                        .Single(t => t.IsEnum && t.Name == name)
                )
                .SelectMany(t => Enum.GetValues(t).Cast<Enum>()),
        ];

    [Theory]
    [MemberData(nameof(EveryAccountEndpointResultCode))]
    public void ApiStatusCode_IsDeliberate_ForEveryAccountEndpointResultCode(Enum resultCode)
    {
        var name = resultCode.ToString();

        Assert.True(
            resultCode.ApiStatusCode() != StatusCodes.Status409Conflict
                || ConflictCodes.Contains(name),
            $"{resultCode.GetType().Name}.{name} fell through to 409: map it or list it here"
        );
    }

    [Theory]
    [InlineData(RetrieveResultCode.Success, StatusCodes.Status200OK)]
    [InlineData(RetrieveResultCode.NotFoundError, StatusCodes.Status404NotFound)]
    [InlineData(RetrieveResultCode.UnauthorizedError, StatusCodes.Status403Forbidden)]
    public void ApiStatusCode_Maps_ForRetrieveResults(RetrieveResultCode code, int status) =>
        Assert.Equal(status, code.ApiStatusCode());
}
