using System.Security.Cryptography;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Entities;
using Corely.Security.Encryption;
using Corely.Security.Encryption.Factories;
using Corely.Security.Signature;
using Microsoft.IdentityModel.Tokens;

namespace Corely.IAM.Security.Mappers;

internal static class AsymmetricKeyMapper
{
    private const string RS256 = "RS256";
    private const string RSA_OAEP_256 = "RSA-OAEP-256";
    private const string ES256 = "ES256";
    private const string P_256 = "P-256";

    extension(AsymmetricKey asymmetricKey)
    {
        public UserAsymmetricKeyEntity ToUserEntity(Guid userId)
        {
            return new UserAsymmetricKeyEntity
            {
                Id = asymmetricKey.Id,
                UserId = userId,
                KeyUsedFor = asymmetricKey.KeyUsedFor,
                ProviderName = asymmetricKey.ProviderName,
                Version = asymmetricKey.Version,
                PublicKey = asymmetricKey.PublicKey,
                EncryptedPrivateKey = asymmetricKey.PrivateKey.ToEncryptedString()!,
                CreatedUtc = asymmetricKey.CreatedUtc,
                ModifiedUtc = asymmetricKey.ModifiedUtc,
            };
        }

        public AccountAsymmetricKeyEntity ToAccountEntity(Guid accountId)
        {
            return new AccountAsymmetricKeyEntity
            {
                Id = asymmetricKey.Id,
                AccountId = accountId,
                KeyUsedFor = asymmetricKey.KeyUsedFor,
                ProviderName = asymmetricKey.ProviderName,
                Version = asymmetricKey.Version,
                PublicKey = asymmetricKey.PublicKey,
                EncryptedPrivateKey = asymmetricKey.PrivateKey.ToEncryptedString()!,
                CreatedUtc = asymmetricKey.CreatedUtc,
                ModifiedUtc = asymmetricKey.ModifiedUtc,
            };
        }
    }

    extension(UserAsymmetricKeyEntity entity)
    {
        public AsymmetricKey ToModel(ISymmetricEncryptionProviderFactory encryptionProviderFactory)
        {
            return new AsymmetricKey
            {
                Id = entity.Id,
                KeyUsedFor = entity.KeyUsedFor,
                ProviderName = entity.ProviderName,
                Version = entity.Version,
                PublicKey = entity.PublicKey,
                PrivateKey = entity.EncryptedPrivateKey.ToEncryptedValue(encryptionProviderFactory),
                CreatedUtc = entity.CreatedUtc,
                ModifiedUtc = entity.ModifiedUtc,
            };
        }
    }

    extension(AccountAsymmetricKeyEntity entity)
    {
        public AsymmetricKey ToModel(ISymmetricEncryptionProviderFactory encryptionProviderFactory)
        {
            return new AsymmetricKey
            {
                Id = entity.Id,
                KeyUsedFor = entity.KeyUsedFor,
                ProviderName = entity.ProviderName,
                Version = entity.Version,
                PublicKey = entity.PublicKey,
                PrivateKey = entity.EncryptedPrivateKey.ToEncryptedValue(encryptionProviderFactory),
                CreatedUtc = entity.CreatedUtc,
                ModifiedUtc = entity.ModifiedUtc,
            };
        }

        public AccountJsonWebKey? ToAccountJsonWebKey(Guid accountId)
        {
            var use =
                entity.KeyUsedFor == KeyUsedFor.Signature
                    ? AccountJsonWebKey.USE_SIGNATURE
                    : AccountJsonWebKey.USE_ENCRYPTION;
            var keyId = AccountJsonWebKey.KeyIdFor(accountId, use, entity.Version);
            var publicKey = Convert.FromBase64String(entity.PublicKey);

            return entity.ProviderName switch
            {
                AsymmetricSignatureConstants.RSA_SHA256_CODE
                or AsymmetricSignatureConstants.LEGACY_RSA_SHA256_CODE => RsaJsonWebKey(
                    publicKey,
                    use,
                    keyId,
                    RS256
                ),
                AsymmetricEncryptionConstants.RSA_CODE
                or AsymmetricEncryptionConstants.LEGACY_RSA_CODE => RsaJsonWebKey(
                    publicKey,
                    use,
                    keyId,
                    RSA_OAEP_256
                ),
                AsymmetricSignatureConstants.ECDSA_SHA256_CODE
                or AsymmetricSignatureConstants.LEGACY_ECDSA_SHA256_CODE => EcJsonWebKey(
                    publicKey,
                    use,
                    keyId
                ),
                _ => null,
            };
        }
    }

    private static AccountJsonWebKey RsaJsonWebKey(
        byte[] publicKey,
        string use,
        string keyId,
        string algorithm
    )
    {
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(publicKey, out _);
        var parameters = rsa.ExportParameters(includePrivateParameters: false);
        return new AccountJsonWebKey
        {
            KeyType = AccountJsonWebKey.KEY_TYPE_RSA,
            Use = use,
            KeyId = keyId,
            Algorithm = algorithm,
            Modulus = Base64UrlEncoder.Encode(parameters.Modulus),
            Exponent = Base64UrlEncoder.Encode(parameters.Exponent),
        };
    }

    private static AccountJsonWebKey? EcJsonWebKey(byte[] publicKey, string use, string keyId)
    {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);
        var parameters = ecdsa.ExportParameters(includePrivateParameters: false);
        if (parameters.Curve.Oid.Value != ECCurve.NamedCurves.nistP256.Oid.Value)
            return null;

        return new AccountJsonWebKey
        {
            KeyType = AccountJsonWebKey.KEY_TYPE_ELLIPTIC_CURVE,
            Use = use,
            KeyId = keyId,
            Algorithm = ES256,
            Curve = P_256,
            X = Base64UrlEncoder.Encode(parameters.Q.X),
            Y = Base64UrlEncoder.Encode(parameters.Q.Y),
        };
    }
}
