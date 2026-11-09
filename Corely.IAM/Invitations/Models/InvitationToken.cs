namespace Corely.IAM.Invitations.Models;

internal sealed record InvitationToken(Guid InvitationId, string Secret)
{
    public override string ToString() => $"{InvitationId:N}.{Secret}";

    public static bool TryParse(string? token, out InvitationToken invitationToken)
    {
        invitationToken = new(Guid.Empty, string.Empty);

        if (string.IsNullOrWhiteSpace(token))
            return false;

        var parts = token.Trim().Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return false;

        if (!Guid.TryParseExact(parts[0], "N", out var invitationId))
            return false;

        if (string.IsNullOrWhiteSpace(parts[1]))
            return false;

        invitationToken = new(invitationId, parts[1]);
        return true;
    }
}
