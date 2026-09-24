namespace Corely.IAM.Extensions;

internal static class StringExtensions
{
    extension(string value)
    {
        public byte[] FromBase32()
        {
            var cleanInput = value.TrimEnd('=').ToUpperInvariant();
            var output = new byte[cleanInput.Length * 5 / 8];
            var buffer = 0;
            var bitsLeft = 0;
            var index = 0;

            foreach (var c in cleanInput)
            {
                var digit = c switch
                {
                    >= 'A' and <= 'Z' => c - 'A',
                    >= '2' and <= '7' => c - '2' + 26,
                    _ => throw new ArgumentException($"Invalid base32 character: {c}"),
                };

                buffer = (buffer << 5) | digit;
                bitsLeft += 5;

                if (bitsLeft >= 8)
                {
                    bitsLeft -= 8;
                    output[index++] = (byte)(buffer >> bitsLeft);
                }
            }

            return output;
        }

        public string ToDisplayRecoveryCode() => $"{value[..4]}-{value[4..]}";
    }
}
