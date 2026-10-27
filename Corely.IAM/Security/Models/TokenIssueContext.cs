using Corely.IAM.Accounts.Models;
using Corely.IAM.Users.Entities;

namespace Corely.IAM.Security.Models;

internal sealed record TokenIssueContext(
    UserEntity UserEntity,
    UserAsymmetricKeyEntity SignatureKey,
    List<Account> Accounts,
    Account? SignedInAccount
);
