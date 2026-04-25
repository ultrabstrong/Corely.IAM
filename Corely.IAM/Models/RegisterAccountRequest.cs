namespace Corely.IAM.Models;

public record RegisterAccountRequest(string AccountName, Guid OwnerUserId);
