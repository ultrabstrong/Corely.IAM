using System.Security.Cryptography;
using System.Text;

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
        return Base32Encode(bytes);
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
        var keyBytes = Base32Decode(base32Secret);
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

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var sb = new StringBuilder((data.Length * 8 + 4) / 5);
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                bitsLeft -= 5;
                sb.Append(alphabet[(buffer >> bitsLeft) & 0x1F]);
            }
        }

        if (bitsLeft > 0)
        {
            sb.Append(alphabet[(buffer << (5 - bitsLeft)) & 0x1F]);
        }

        return sb.ToString();
    }

    private static byte[] Base32Decode(string base32)
    {
        var cleanInput = base32.TrimEnd('=').ToUpperInvariant();
        var output = new byte[cleanInput.Length * 5 / 8];
        var buffer = 0;
        var bitsLeft = 0;
        var index = 0;

        foreach (var c in cleanInput)
        {
            var value = c switch
            {
                >= 'A' and <= 'Z' => c - 'A',
                >= '2' and <= '7' => c - '2' + 26,
                _ => throw new ArgumentException($"Invalid base32 character: {c}"),
            };

            buffer = (buffer << 5) | value;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                output[index++] = (byte)(buffer >> bitsLeft);
            }
        }

        return output;
    }
}
