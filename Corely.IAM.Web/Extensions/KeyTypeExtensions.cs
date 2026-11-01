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

        public string CompromiseWarning() =>
            keyType switch
            {
                KeyType.SymmetricEncryption or KeyType.AsymmetricEncryption =>
                    "This cannot be undone. The old key stops being used for anything new, but it still decrypts what it encrypted, so nothing is lost. Anyone holding a copy can still read that data, so re-encrypting moves it onto the new key here but does not take back what they already have.",
                KeyType.AsymmetricSignature =>
                    "This cannot be undone. Signatures made with the old key stop verifying here and it leaves the published key set, so anything signed with it must be signed again. API key sessions signed with it end at once and must renew.",
                _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null),
            };

        public string CompromisedNotice() =>
            keyType switch
            {
                KeyType.SymmetricEncryption or KeyType.AsymmetricEncryption =>
                    "Marked versions only decrypt what they already encrypted. Re-encrypt moves that data onto the current key.",
                KeyType.AsymmetricSignature =>
                    "Marked versions are no longer trusted: their signatures do not verify and they are not published.",
                _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null),
            };

        public string OlderVersionNotice() =>
            keyType switch
            {
                KeyType.SymmetricEncryption or KeyType.AsymmetricEncryption =>
                    "This is an older version. New encryption only uses the current version. Values encrypted with this version still decrypt with Decrypt on the current version.",
                KeyType.AsymmetricSignature =>
                    "This is an older version. New signatures only use the current version. Signatures made with this version still verify with Verify on the current version.",
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
