using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.Security.Providers;
using Corely.Security.Encryption;
using Corely.Security.Hashing;
using Corely.Security.Signature;
using Microsoft.Extensions.Configuration;

namespace Corely.IAM;

public class IAMOptions
{
    public ISecurityConfigurationProvider SecurityConfigurationProvider { get; private set; } =
        null!;

    internal IConfiguration Configuration { get; private set; } = null!;

    internal Func<IServiceProvider, IEFConfiguration>? EFConfigurationFactory { get; private set; }

    internal string SymmetricEncryptionCode { get; private set; } =
        SymmetricEncryptionConstants.AES_CODE;

    internal string AsymmetricEncryptionCode { get; private set; } =
        AsymmetricEncryptionConstants.RSA_CODE;

    internal string AsymmetricSignatureCode { get; private set; } =
        AsymmetricSignatureConstants.ECDSA_SHA256_CODE;

    internal string HashCode { get; private set; } = HashConstants.SALTED_SHA256_CODE;

    internal Dictionary<string, string> CustomResourceTypes { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    private IAMOptions() { }

    public static IAMOptions Create(
        IConfiguration configuration,
        ISecurityConfigurationProvider securityConfigurationProvider
    )
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(securityConfigurationProvider);
        return new IAMOptions
        {
            Configuration = configuration,
            SecurityConfigurationProvider = securityConfigurationProvider,
        };
    }

    public static IAMOptions Create(
        IConfiguration configuration,
        ISecurityConfigurationProvider securityConfigurationProvider,
        Func<IServiceProvider, IEFConfiguration> efConfigurationFactory
    )
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(securityConfigurationProvider);
        ArgumentNullException.ThrowIfNull(efConfigurationFactory);
        return new IAMOptions
        {
            Configuration = configuration,
            SecurityConfigurationProvider = securityConfigurationProvider,
            EFConfigurationFactory = efConfigurationFactory,
        };
    }

    public IAMOptions RegisterResourceType(string name, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        CustomResourceTypes[name] = description;
        return this;
    }

    public IAMOptions UseSymmetricEncryption(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        SymmetricEncryptionCode = code;
        return this;
    }

    public IAMOptions UseAsymmetricEncryption(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        AsymmetricEncryptionCode = code;
        return this;
    }

    public IAMOptions UseAsymmetricSignature(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        AsymmetricSignatureCode = code;
        return this;
    }

    public IAMOptions UseHash(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        HashCode = code;
        return this;
    }
}
