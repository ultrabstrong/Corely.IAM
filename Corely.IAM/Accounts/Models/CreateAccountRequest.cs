namespace Corely.IAM.Accounts.Models;

internal record CreateAccountRequest(string AccountName, Guid OwnerUserId);
