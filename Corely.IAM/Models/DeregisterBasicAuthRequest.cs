namespace Corely.IAM.Models;

public record DeregisterBasicAuthRequest(string CurrentPassword, string? TotpCode = null);
