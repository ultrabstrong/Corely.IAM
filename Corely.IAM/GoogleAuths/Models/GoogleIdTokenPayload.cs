namespace Corely.IAM.GoogleAuths.Models;

internal record GoogleIdTokenPayload(
    string Subject,
    string Email,
    bool EmailVerified,
    string? Nonce = null,
    DateTime? IssuedUtc = null
)
{
    internal const int REAUTHENTICATION_MAX_AGE_SECONDS = 300;
    internal const int ISSUED_AT_SKEW_SECONDS = 60;

    public bool ProvesAFreshSignIn(string? expectedNonce, DateTime utcNow) =>
        !string.IsNullOrWhiteSpace(expectedNonce)
        && Nonce == expectedNonce
        && IssuedUtc is { } issuedUtc
        && issuedUtc <= utcNow.AddSeconds(ISSUED_AT_SKEW_SECONDS)
        && issuedUtc >= utcNow.AddSeconds(-REAUTHENTICATION_MAX_AGE_SECONDS);
}
