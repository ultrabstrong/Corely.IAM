using Corely.IAM.DevTools.Attributes;
using Corely.Security.KeyStore;
using Corely.Security.Signature;
using Corely.Security.Signature.Factories;

namespace Corely.IAM.DevTools.Commands;

internal class AsymmetricSignature : CommandBase
{
    private const string DEFAULT_SIGNATURE_TYPE = AsymmetricSignatureConstants.ECDSA_SHA256_CODE;

    private readonly AsymmetricSignatureProviderFactory _signatureProviderFactory = new(
        DEFAULT_SIGNATURE_TYPE
    );

    [Argument(
        "File with keys to sign message (default), validate (-v flag), or decrypt value (-d flag). Format public<newline>private",
        false
    )]
    private string KeyFile { get; init; } = null!;

    [Argument("Message to sign or verify", false)]
    private string Message { get; init; } = null!;

    [Argument(
        "Code for signature type to use (hint: use -l to list codes. default used if code not provided)",
        false
    )]
    private string SignatureTypeCode { get; init; } = DEFAULT_SIGNATURE_TYPE;

    [Option("-l", "--list", Description = "List asymmetric signature providers")]
    private bool List { get; init; }

    [Option("-c", "--create", Description = "Create new asymmetric keys")]
    private bool Create { get; init; }

    [Option("-s", "--signature", Description = "Signature to verify")]
    private string Signature { get; init; } = null!;

    [Option("-v", "--validate", Description = "Validate a key")]
    private bool Validate { get; init; }

    public AsymmetricSignature()
        : base(
            "asym-sign",
            "Asymmetric signature operations",
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
            CreateKeys();
        }
        if (!string.IsNullOrEmpty(Message))
        {
            if (string.IsNullOrEmpty(Signature))
            {
                Sign();
            }
            else
            {
                Verify();
            }
        }
        if (Validate)
        {
            ValidateKey();
        }

        if (
            !List
            && !Create
            && string.IsNullOrEmpty(Message)
            && string.IsNullOrEmpty(Signature)
            && !Validate
        )
        {
            ShowHelp();
        }
    }

    private (string publicKey, string privateKey) ReadKeysFromFile()
    {
        var keys = File.ReadAllLines(KeyFile);
        if (keys.Length != 2)
        {
            throw new Exception("Invalid key file format. Must be public<newline>private");
        }

        return (keys[0], keys[1]);
    }

    private void ListProviders()
    {
        var providers = _signatureProviderFactory.ListProviders();
        foreach (var (providerCode, providerType) in providers)
        {
            Console.WriteLine(
                $"Code {providerCode} = {providerType.Name} {(providerCode == DEFAULT_SIGNATURE_TYPE ? "(default)" : "")}"
            );
        }
    }

    private void CreateKeys()
    {
        var asymmetricEncryptionProvider = _signatureProviderFactory.GetProvider(SignatureTypeCode);
        var (publicKey, privateKey) = asymmetricEncryptionProvider
            .GetAsymmetricKeyProvider()
            .CreateKeys();
        File.WriteAllText(KeyFile, $"{publicKey}{Environment.NewLine}{privateKey}");
        Console.WriteLine($"Keys written to {KeyFile}");
    }

    private void ValidateKey()
    {
        var asymmetricEncryptionProvider = _signatureProviderFactory.GetProvider(SignatureTypeCode);
        var (publicKey, privateKey) = ReadKeysFromFile();
        var isValid = asymmetricEncryptionProvider
            .GetAsymmetricKeyProvider()
            .IsKeyValid(publicKey, privateKey);
        Console.WriteLine(isValid ? "Keys are valid" : "Keys are not valid");
    }

    private void Sign()
    {
        var (publicKey, privateKey) = ReadKeysFromFile();
        var keyProvider = new InMemoryAsymmetricKeyStoreProvider(publicKey, privateKey);
        var signature = _signatureProviderFactory
            .GetProvider(SignatureTypeCode)
            .Sign(Message, keyProvider);
        Console.WriteLine(signature);
    }

    private void Verify()
    {
        var (publicKey, privateKey) = ReadKeysFromFile();
        var keyProvider = new InMemoryAsymmetricKeyStoreProvider(publicKey, privateKey);
        var isValid = _signatureProviderFactory
            .GetProvider(SignatureTypeCode)
            .Verify(Message, Signature, keyProvider);
        Console.WriteLine(isValid ? "Signature is valid" : "Signature is not valid");
    }
}
