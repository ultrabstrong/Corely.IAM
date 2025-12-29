using Corely.IAM.Groups.Models;

namespace Corely.IAM.Models;

public record RegisterUsersWithGroupResult(
    AddUsersToGroupResultCode ResultCode,
    string Message,
    int RegisteredUserCount,
    List<Guid> InvalidUserIds = null
);
