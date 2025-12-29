using Corely.IAM.Groups.Models;

namespace Corely.IAM.Models;

public record RegisterGroupResult(
    CreateGroupResultCode ResultCode,
    string? Message,
    Guid CreatedGroupId
);
