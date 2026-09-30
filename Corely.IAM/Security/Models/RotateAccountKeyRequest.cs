using Corely.IAM.Security.Enums;

namespace Corely.IAM.Security.Models;

public record RotateAccountKeyRequest(Guid AccountId, KeyType KeyType);
