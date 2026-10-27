using Corely.IAM.Security.Enums;

namespace Corely.IAM.Security.Models;

internal sealed record SessionTokenClaims(
    TokenPrincipalType PrincipalType,
    Guid SubjectId,
    Guid TokenId,
    string DeviceId,
    Guid? SignedInAccountId
);
