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

        public string CopyKeyLabel() =>
            keyType switch
            {
                KeyType.SymmetricEncryption => "Copy key",
                KeyType.AsymmetricEncryption or KeyType.AsymmetricSignature => "Copy private key",
                _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null),
            };

        public string KeyFormat() =>
            keyType switch
            {
                KeyType.SymmetricEncryption => "Base64",
                KeyType.AsymmetricEncryption or KeyType.AsymmetricSignature => "Base64 PKCS#8",
                _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null),
            };

        public IReadOnlyList<string> CopyKeyWarnings() =>
            keyType switch
            {
                KeyType.SymmetricEncryption =>
                [
                    "You usually do not need this. This app handles encryption for you.",
                    "Anyone holding this key can read data encrypted with it, as this account.",
                    SECRET_KEY_WARNING,
                    COPY_NOT_CANCELLED_WARNING,
                ],
                KeyType.AsymmetricEncryption =>
                [
                    "You usually do not need this. This app handles encryption for you.",
                    "Anyone holding this key can read data encrypted to this account.",
                    SECRET_KEY_WARNING,
                    COPY_NOT_CANCELLED_WARNING,
                ],
                KeyType.AsymmetricSignature =>
                [
                    "You usually do not need this. This app handles signing for you.",
                    "Anyone holding this key can sign as this account.",
                    SECRET_KEY_WARNING,
                    "Rotating later does not cancel this copy: signatures made with it keep verifying.",
                ],
                _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null),
            };
    }

    private const string SECRET_KEY_WARNING =
        "This key is a secret. Store it somewhere secure, and do not share it with anyone you do not trust.";

    private const string COPY_NOT_CANCELLED_WARNING =
        "Rotating later does not cancel this copy: it still reads what was encrypted with it.";
}
