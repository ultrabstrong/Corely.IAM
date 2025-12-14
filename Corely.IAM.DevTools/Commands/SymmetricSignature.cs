using Corely.IAM.DevTools.Attributes;
using Corely.Security.KeyStore;
using Corely.Security.Signature;
using Corely.Security.Signature.Factories;

namespace Corely.IAM.DevTools.Commands;

internal class SymmetricSignature : CommandBase
{
    private const string DEFAULT_SIGNATURE_TYPE = SymmetricSignatureConstants.HMAC_SHA256_CODE;

    private readonly SymmetricSignatureProviderFactory _signatureProviderFactory = new(
        DEFAULT_SIGNATURE_TYPE
    );

    [Argument(
        "Key to sign message (default), validate (-v flag), or verify signature (-s flag)",
        false
    )]
    private string Key { get; init; } = null!;

    [Argument("Message to sign or verify", false)]
    private string Message { get; init; } = null!;

    [Argument(
        "Code for signature type to use (hint: use -l to list codes. default used if code not provided)",
        false
    )]
    private string SignatureTypeCode { get; init; } = DEFAULT_SIGNATURE_TYPE;

    [Option("-l", "--list", Description = "List symmetric signature providers")]
    private bool List { get; init; }

    [Option("-c", "--create", Description = "Create a new symmetric key")]
    private bool Create { get; init; }

    [Option("-s", "--signature", Description = "Signature to verify")]
    private string Signature { get; init; } = null!;

    [Option("-v", "--validate", Description = "Validate a key")]
    private bool Validate { get; init; }

    public SymmetricSignature()
        : base(
            "sym-sign",
            "Symmetric signature operations",
            "Use at least one flag to perform an operation"
        )
    { }

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

    private void CreateKey()
    {
        var signatureProvider = _signatureProviderFactory.GetProvider(SignatureTypeCode);
        var key = signatureProvider.GetSymmetricKeyProvider().CreateKey();
        Console.WriteLine(key);
    }

    private void ValidateKey()
    {
        var signatureProvider = _signatureProviderFactory.GetProvider(SignatureTypeCode);
        var isValid = signatureProvider.GetSymmetricKeyProvider().IsKeyValid(Key);
        Console.WriteLine($"Key is {(isValid ? "valid" : "invalid")}");
    }

    private void Sign()
    {
        var keyProvider = new InMemorySymmetricKeyStoreProvider(Key);
        var signature = _signatureProviderFactory
            .GetProvider(SignatureTypeCode)
            .Sign(Message, keyProvider);
        Console.WriteLine(signature);
    }

    private void Verify()
    {
        var keyProvider = new InMemorySymmetricKeyStoreProvider(Key);
        var isValid = _signatureProviderFactory
            .GetProvider(SignatureTypeCode)
            .Verify(Message, Signature, keyProvider);
        Console.WriteLine($"Signature is {(isValid ? "valid" : "invalid")} for message");
    }
}
