namespace Corely.IAM.PasswordRecoveries.Models;

public record ResetPasswordWithRecoveryRequest(string Token, string Password);
