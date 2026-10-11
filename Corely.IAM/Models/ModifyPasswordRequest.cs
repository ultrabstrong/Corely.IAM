namespace Corely.IAM.Models;

public record ModifyPasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string? TotpCode = null
);
