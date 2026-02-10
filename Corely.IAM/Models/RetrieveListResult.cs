namespace Corely.IAM.Models;

public record RetrieveListResult<T>(
    RetrieveResultCode ResultCode,
    string Message,
    PagedResult<T>? Data
);
