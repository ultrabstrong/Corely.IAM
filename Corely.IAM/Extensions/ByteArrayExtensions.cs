using System.Text;

namespace Corely.IAM.Extensions;

internal static class ByteArrayExtensions
{
    extension(byte[] data)
    {
        public string ToBase32()
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
    }
}
