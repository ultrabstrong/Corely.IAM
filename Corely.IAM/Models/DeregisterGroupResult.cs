namespace Corely.IAM.Models;

public record DeregisterGroupResult(DeregisterGroupResultCode ResultCode, string Message);

public enum DeregisterGroupResultCode
{
    Success,
    GroupNotFoundError,
}
