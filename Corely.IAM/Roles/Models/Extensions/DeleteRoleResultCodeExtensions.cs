using Corely.IAM.Models;

namespace Corely.IAM.Roles.Models.Extensions;

internal static class DeleteRoleResultCodeExtensions
{
    extension(DeleteRoleResultCode resultCode)
    {
        public DeregisterRoleResultCode ToDeregisterRoleResultCode() =>
            resultCode switch
            {
                DeleteRoleResultCode.Success => DeregisterRoleResultCode.Success,
                DeleteRoleResultCode.RoleNotFoundError =>
                    DeregisterRoleResultCode.RoleNotFoundError,
                DeleteRoleResultCode.SystemDefinedRoleError =>
                    DeregisterRoleResultCode.SystemDefinedRoleError,
                DeleteRoleResultCode.UnauthorizedError =>
                    DeregisterRoleResultCode.UnauthorizedError,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(resultCode),
                    resultCode,
                    $"Unmapped {nameof(DeleteRoleResultCode)}: {resultCode}"
                ),
            };
    }
}
