namespace Corely.IAM.Models;

internal record GetResult<T>(RetrieveResultCode ResultCode, string Message, T? Data);
