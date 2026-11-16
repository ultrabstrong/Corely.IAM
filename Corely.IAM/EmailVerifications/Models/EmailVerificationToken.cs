namespace Corely.IAM.EmailVerifications.Models;

internal sealed record EmailVerificationToken(Guid Id, string Secret)
{
    public override string ToString() => $"{Id:N}.{Secret}";

    public static bool TryParse(string? token, out EmailVerificationToken parsed)
    {
        parsed = new(Guid.Empty, string.Empty);

        if (string.IsNullOrWhiteSpace(token))
            return false;

        var parts = token.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[1]))
            return false;

        if (!Guid.TryParseExact(parts[0], "N", out var id))
            return false;

        parsed = new(id, parts[1]);
        return true;
    }
}
