namespace Corely.IAM.Models;

public record DeregisterAccountRequest(int AccountId, string OwnerUserPassword);
