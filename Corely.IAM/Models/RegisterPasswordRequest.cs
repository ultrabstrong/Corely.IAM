namespace Corely.IAM.Models;

public record RegisterPasswordRequest(string Password, string? TotpCode = null);
