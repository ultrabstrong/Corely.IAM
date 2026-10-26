namespace Corely.IAM.Users.Models;

internal sealed record RefreshToken(Guid TokenId, string Secret)
{
    public override string ToString() => $"{TokenId:N}.{Secret}";

    public static bool TryParse(string? token, out RefreshToken refreshToken)
    {
        refreshToken = new(Guid.Empty, string.Empty);

        if (string.IsNullOrWhiteSpace(token))
            return false;

        var parts = token.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return false;

        if (!Guid.TryParseExact(parts[0], "N", out var tokenId))
            return false;

        if (string.IsNullOrWhiteSpace(parts[1]))
            return false;

        refreshToken = new(tokenId, parts[1]);
        return true;
    }
}
