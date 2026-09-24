namespace Corely.IAM.PasswordRecoveries.Models;

internal sealed record PasswordRecoveryToken(Guid RecoveryId, string Secret)
{
    public override string ToString() => $"{RecoveryId:N}.{Secret}";

    public static bool TryParse(string? token, out PasswordRecoveryToken recoveryToken)
    {
        recoveryToken = new(Guid.Empty, string.Empty);

        if (string.IsNullOrWhiteSpace(token))
            return false;

        var parts = token.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return false;

        if (!Guid.TryParseExact(parts[0], "N", out var recoveryId))
            return false;

        if (string.IsNullOrWhiteSpace(parts[1]))
            return false;

        recoveryToken = new(recoveryId, parts[1]);
        return true;
    }
}
