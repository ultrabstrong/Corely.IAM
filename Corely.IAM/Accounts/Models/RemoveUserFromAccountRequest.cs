namespace Corely.IAM.Accounts.Models;

public record RemoveUserFromAccountRequest(Guid UserId, Guid AccountId);
