using Corely.IAM.Roles.Models;

namespace Corely.IAM.Models;

public record RegisterRoleResult(
    CreateRoleResultCode ResultCode,
    string Message,
    int CreatedRoleId
);
