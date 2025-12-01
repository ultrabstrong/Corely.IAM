using Corely.IAM.Enums;

namespace Corely.IAM.BasicAuths.Models;

public enum UpsertBasicAuthResultCode
{
    Success,
    Failure,
}

internal record UpsertBasicAuthResult(
    UpsertBasicAuthResultCode ResultCode,
    string? Message,
    int CreatedId,
    UpsertType UpsertType
);
