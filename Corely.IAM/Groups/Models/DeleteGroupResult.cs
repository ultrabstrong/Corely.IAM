namespace Corely.IAM.Groups.Models;

public record DeleteGroupResult(DeleteGroupResultCode ResultCode, string Message);

public enum DeleteGroupResultCode
{
    Success,
    GroupNotFoundError,
    GroupHasSoleOwnersError,
    UnauthorizedError,
}
