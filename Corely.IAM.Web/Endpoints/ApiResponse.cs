namespace Corely.IAM.Web.Endpoints;

public static class ApiResponse
{
    public const string SUCCESS = "Success";

    public static ApiResponse<T> Success<T>(T data) => new(SUCCESS, null, data);

    public static ApiResponse<object> Success() => new(SUCCESS, null, null);

    public static ApiResponse<object> Failure(string resultCode, string? message) =>
        new(resultCode, message, null);
}
