namespace Corely.IAM.Web.Endpoints;

public sealed record ApiResponse<T>(string ResultCode, string? Message, T? Data);
