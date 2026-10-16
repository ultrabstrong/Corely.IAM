namespace Corely.IAM.Groups.Models;

internal record DeleteGroupResult(DeleteGroupResultCode ResultCode, string Message);

internal enum DeleteGroupResultCode
{
    Success,
    GroupNotFoundError,
    GroupHasSoleOwnersError,
    UnauthorizedError,
}
