namespace Corely.IAM.Accounts.Models;

internal record RemoveUserFromAccountRequest(Guid UserId, Guid AccountId);
