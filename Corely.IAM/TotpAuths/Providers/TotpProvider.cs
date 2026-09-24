using System.Security.Cryptography;
using Corely.IAM.Extensions;

namespace Corely.IAM.TotpAuths.Providers;

internal class TotpProvider(TimeProvider timeProvider) : ITotpProvider
{
    private const int SECRET_BYTES = 20;
    private const int DIGITS = 6;
    private const int PERIOD_SECONDS = 30;
    private const int TOLERANCE_STEPS = 1;
    private static readonly int[] _pow10 = [1, 10, 100, 1000, 10000, 100000, 1000000];

    public string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(SECRET_BYTES);
        return bytes.ToBase32();
    }

    public string GenerateSetupUri(string secret, string issuer, string userLabel)
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedLabel = Uri.EscapeDataString(userLabel);
        return $"otpauth://totp/{encodedIssuer}:{encodedLabel}?secret={secret}&issuer={encodedIssuer}&algorithm=SHA1&digits={DIGITS}&period={PERIOD_SECONDS}";
    }

    public string GenerateCode(string secret)
    {
        var timeStep = GetCurrentTimeStep();
        return ComputeCode(secret, timeStep);
    }

    public bool ValidateCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != DIGITS)
            return false;

        var timeStep = GetCurrentTimeStep();
        for (var i = -TOLERANCE_STEPS; i <= TOLERANCE_STEPS; i++)
        {
            var candidateCode = ComputeCode(secret, timeStep + i);
            if (string.Equals(code, candidateCode, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private long GetCurrentTimeStep()
    {
        var unixTime = timeProvider.GetUtcNow().ToUnixTimeSeconds();
        return unixTime / PERIOD_SECONDS;
    }

    private static string ComputeCode(string base32Secret, long timeStep)
    {
        var keyBytes = base32Secret.FromBase32();
        var timeBytes = BitConverter.GetBytes(timeStep);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(timeBytes);

        using var hmac = new HMACSHA1(keyBytes);
        var hash = hmac.ComputeHash(timeBytes);

        var offset = hash[^1] & 0x0F;
        var binaryCode =
            ((hash[offset] & 0x7F) << 24)
            | ((hash[offset + 1] & 0xFF) << 16)
            | ((hash[offset + 2] & 0xFF) << 8)
            | (hash[offset + 3] & 0xFF);

        var otp = binaryCode % _pow10[DIGITS];
        return otp.ToString().PadLeft(DIGITS, '0');
    }
}
