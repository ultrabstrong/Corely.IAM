using Corely.IAM.Models;

namespace Corely.IAM.Groups.Models.Extensions;

internal static class DeleteGroupResultCodeExtensions
{
    extension(DeleteGroupResultCode resultCode)
    {
        public DeregisterGroupResultCode ToDeregisterGroupResultCode() =>
            resultCode switch
            {
                DeleteGroupResultCode.Success => DeregisterGroupResultCode.Success,
                DeleteGroupResultCode.GroupNotFoundError =>
                    DeregisterGroupResultCode.GroupNotFoundError,
                DeleteGroupResultCode.GroupHasSoleOwnersError =>
                    DeregisterGroupResultCode.GroupHasSoleOwnersError,
                DeleteGroupResultCode.UnauthorizedError =>
                    DeregisterGroupResultCode.UnauthorizedError,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(resultCode),
                    resultCode,
                    $"Unmapped {nameof(DeleteGroupResultCode)}: {resultCode}"
                ),
            };
    }
}
