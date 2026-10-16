namespace Corely.IAM.Users.Models;

internal record IsSoleOwnerOfAccountResult(
    bool IsSoleOwner,
    bool UserHasOwnerRole,
    bool HasSingleOwnershipSource = true
);
