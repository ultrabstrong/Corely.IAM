using Corely.IAM.DevTools.Attributes;
using Corely.Security.Hashing.Factories;

namespace Corely.IAM.DevTools.Commands;

internal class SaltedHash : CommandBase
{
    [Argument("Hash type to use", false)]
    private string HashTypeCode { get; init; } = null!;

    [Argument("Value to hash", false)]
    private string Value { get; init; } = null!;

    public SaltedHash()
        : base(
            "shash",
            "Salted hash operations",
            "Default: List hash types if no argument or option is provided"
        )
    { }

    protected override void Execute()
    {
        if (string.IsNullOrWhiteSpace(HashTypeCode))
        {
            ListProviders();
        }
        else
        {
            Hash();
        }
    }

    private void ListProviders()
    {
        var hashProviderFactor = new HashProviderFactory(HashTypeCode);
        var providers = hashProviderFactor.ListProviders();
        foreach (var (providerCode, providerType) in providers)
        {
            Console.WriteLine($"Code {providerCode} = {providerType.Name}");
        }
    }

    private void Hash()
    {
        var hashProviderFactor = new HashProviderFactory(HashTypeCode);
        var hashProvider = hashProviderFactor.GetProvider(HashTypeCode);
        var hash = hashProvider.Hash(Value);
        Console.WriteLine($"Hashed value: {hash}");
    }
}
