using Corely.IAM.DevTools.Attributes;
using Corely.Security.Encryption;
using Corely.Security.Encryption.Factories;
using Corely.Security.KeyStore;

namespace Corely.IAM.DevTools.Commands;

internal class SymmetricEncryption : CommandBase
{
    private const string DEFAULT_ENCRYPTION_TYPE = SymmetricEncryptionConstants.AES_CODE;

    private readonly SymmetricEncryptionProviderFactory _encryptionProviderFactory = new(
        DEFAULT_ENCRYPTION_TYPE
    );

    [Argument(
        "Key to validate (default), encrypt value (-e flag), or decrypt value (-d flag)",
        false
    )]
    private string Key { get; init; } = null!;

    [Argument(
        "Code for encryption type to use (hint: use -l to list codes. default used if code not provided)",
        false
    )]
    private string EncryptionTypeCode { get; init; } = DEFAULT_ENCRYPTION_TYPE;

    [Option("-l", "--list", Description = "List asymmetric encryption providers")]
    private bool List { get; init; }

    [Option("-c", "--create", Description = "Create a new symmetric key")]
    private bool Create { get; init; }

    [Option("-e", "--encrypt", Description = "Encrypt a value")]
    private string ToEncrypt { get; init; } = null!;

    [Option("-d", "--decrypt", Description = "Decrypt a value")]
    private string ToDecrypt { get; init; } = null!;

    [Option("-v", "--validate", Description = "Validate a key")]
    private bool Validate { get; init; }

    public SymmetricEncryption()
        : base(
            "sym-encrypt",
            "Symmetric encryption operations",
            "Use at least one flag to perform an operation"
        ) { }

    protected override void Execute()
    {
        if (List)
        {
            ListProviders();
        }
        if (Create)
        {
            CreateKey();
        }
        if (!string.IsNullOrEmpty(ToEncrypt))
        {
            Encrypt();
        }
        if (!string.IsNullOrEmpty(ToDecrypt))
        {
            Decrypt();
        }
        if (Validate)
        {
            ValidateKey();
        }

        if (
            !List
            && !Create
            && string.IsNullOrEmpty(ToEncrypt)
            && string.IsNullOrEmpty(ToDecrypt)
            && !Validate
        )
        {
            ShowHelp();
        }
    }

    private void ListProviders()
    {
        var providers = _encryptionProviderFactory.ListProviders();
        foreach (var (providerCode, providerType) in providers)
        {
            Console.WriteLine(
                $"Code {providerCode} = {providerType.Name} {(providerCode == DEFAULT_ENCRYPTION_TYPE ? "(default)" : "")}"
            );
        }
    }

    private void CreateKey()
    {
        var encryptionProvider = _encryptionProviderFactory.GetProvider(EncryptionTypeCode);
        var key = encryptionProvider.GetSymmetricKeyProvider().CreateKey();
        Console.WriteLine(key);
    }

    private void ValidateKey()
    {
        var encryptionProvider = _encryptionProviderFactory.GetProvider(EncryptionTypeCode);
        var isValid = encryptionProvider.GetSymmetricKeyProvider().IsKeyValid(Key);
        Console.WriteLine($"Key is {(isValid ? "valid" : "invalid")}");
    }

    private void Encrypt()
    {
        var keyProvider = new InMemorySymmetricKeyStoreProvider(Key);
        var encrypted = _encryptionProviderFactory
            .GetProvider(EncryptionTypeCode)
            .Encrypt(ToEncrypt, keyProvider);
        Console.WriteLine(encrypted);
    }

    private void Decrypt()
    {
        var keyProvider = new InMemorySymmetricKeyStoreProvider(Key);
        var decrypted = _encryptionProviderFactory
            .GetProvider(EncryptionTypeCode)
            .Decrypt(ToDecrypt, keyProvider);
        Console.WriteLine(decrypted);
    }
}
