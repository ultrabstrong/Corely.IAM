namespace Corely.IAM.Models;

internal record ListResult<T>(RetrieveResultCode ResultCode, string Message, PagedResult<T>? Data)
{
    public RetrieveListResult<T> ToRetrieveListResult() => new(ResultCode, Message, Data);
}
