using Corely.IAM.Security.Enums;

namespace Corely.IAM.Web.Extensions;

internal static class KeyTypeExtensions
{
    extension(KeyType keyType)
    {
        public string RotationWarning() =>
            keyType switch
            {
                KeyType.SymmetricEncryption or KeyType.AsymmetricEncryption =>
                    "Rotate this encryption key? New values are encrypted with the new key. Values encrypted before still decrypt, and Re-encrypt moves them onto the new key.",
                KeyType.AsymmetricSignature =>
                    "Rotate this signing key? New signatures use the new key, and signatures made before still verify here. Anyone who verifies with the old public key needs the new one for new signatures.",
                _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null),
            };
    }
}
