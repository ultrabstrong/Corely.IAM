using Corely.IAM.ApiKeys.Enums;
using Corely.IAM.Security.Entities;
using Corely.IAM.Security.Enums;

namespace Corely.IAM.Security.Models;

internal sealed record SessionTokenSubject(
    TokenPrincipalType PrincipalType,
    Guid SubjectId,
    AsymmetricKeyEntity SignatureKey,
    string DeviceId,
    IReadOnlyList<Guid> AccountIds,
    Guid? SignedInAccountId,
    ApiKeySlot? Slot = null
);
