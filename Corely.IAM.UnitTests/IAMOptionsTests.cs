using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.Security.Providers;
using Corely.Security.Encryption;
using Corely.Security.Hashing;
using Corely.Security.Signature;
using Microsoft.Extensions.Configuration;

namespace Corely.IAM.UnitTests;

public class IAMOptionsTests
{
    private readonly IConfiguration _configuration = new ConfigurationManager();

    [Fact]
    public void Create_WithValidParams_ReturnsInstance()
    {
        var provider = Mock.Of<ISecurityConfigurationProvider>();

        var options = IAMOptions.Create(_configuration, provider);

        Assert.NotNull(options);
        Assert.Same(provider, options.SecurityConfigurationProvider);
    }

    [Fact]
    public void Create_WithNullConfiguration_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            IAMOptions.Create(null!, Mock.Of<ISecurityConfigurationProvider>())
        );
    }

    [Fact]
    public void Create_WithNullProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => IAMOptions.Create(_configuration, null!));
    }

    [Fact]
    public void Create_ConfigurationPropertyIsSet()
    {
        var options = IAMOptions.Create(_configuration, Mock.Of<ISecurityConfigurationProvider>());

        Assert.Same(_configuration, options.Configuration);
    }

    [Fact]
    public void Create_WithoutEFConfig_EFConfigurationFactoryIsNull()
    {
        var options = IAMOptions.Create(_configuration, Mock.Of<ISecurityConfigurationProvider>());

        Assert.Null(options.EFConfigurationFactory);
    }

    [Fact]
    public void Create_WithEFConfig_SetsEFConfigurationFactory()
    {
        Func<IServiceProvider, IEFConfiguration> efFactory = _ => Mock.Of<IEFConfiguration>();

        var options = IAMOptions.Create(
            _configuration,
            Mock.Of<ISecurityConfigurationProvider>(),
            efFactory
        );

        Assert.Same(efFactory, options.EFConfigurationFactory);
    }

    [Fact]
    public void Create_WithEFOverload_WithNullConfiguration_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            IAMOptions.Create(
                null!,
                Mock.Of<ISecurityConfigurationProvider>(),
                _ => Mock.Of<IEFConfiguration>()
            )
        );
    }

    [Fact]
    public void Create_WithEFOverload_WithNullProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            IAMOptions.Create(_configuration, null!, _ => Mock.Of<IEFConfiguration>())
        );
    }

    [Fact]
    public void Create_WithNullEFConfigurationFactory_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            IAMOptions.Create(_configuration, Mock.Of<ISecurityConfigurationProvider>(), null!)
        );
    }

    [Fact]
    public void RegisterResourceType_StoresType()
    {
        var options = IAMOptions.Create(_configuration, Mock.Of<ISecurityConfigurationProvider>());

        options.RegisterResourceType("invoice", "Customer invoices");

        Assert.True(options.CustomResourceTypes.ContainsKey("invoice"));
        Assert.Equal("Customer invoices", options.CustomResourceTypes["invoice"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterResourceType_WithNullOrWhitespaceName_ThrowsArgumentException(string? name)
    {
        var options = IAMOptions.Create(_configuration, Mock.Of<ISecurityConfigurationProvider>());

        Assert.ThrowsAny<ArgumentException>(() =>
            options.RegisterResourceType(name!, "Description")
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterResourceType_WithNullOrWhitespaceDescription_ThrowsArgumentException(
        string? description
    )
    {
        var options = IAMOptions.Create(_configuration, Mock.Of<ISecurityConfigurationProvider>());

        Assert.ThrowsAny<ArgumentException>(() =>
            options.RegisterResourceType("invoice", description!)
        );
    }

    [Fact]
    public void CryptoProperties_HaveCorrectDefaults()
    {
        var options = IAMOptions.Create(_configuration, Mock.Of<ISecurityConfigurationProvider>());

        Assert.Equal(SymmetricEncryptionConstants.AES_CODE, options.SymmetricEncryptionCode);
        Assert.Equal(AsymmetricEncryptionConstants.RSA_CODE, options.AsymmetricEncryptionCode);
        Assert.Equal(
            AsymmetricSignatureConstants.ECDSA_SHA256_CODE,
            options.AsymmetricSignatureCode
        );
        Assert.Equal(HashConstants.SALTED_SHA256_CODE, options.HashCode);
    }

    [Fact]
    public void UseSymmetricEncryption_SetsCode()
    {
        var options = IAMOptions.Create(_configuration, Mock.Of<ISecurityConfigurationProvider>());

        options.UseSymmetricEncryption("custom_sym");

        Assert.Equal("custom_sym", options.SymmetricEncryptionCode);
    }

    [Fact]
    public void UseAsymmetricEncryption_SetsCode()
    {
        var options = IAMOptions.Create(_configuration, Mock.Of<ISecurityConfigurationProvider>());

        options.UseAsymmetricEncryption("custom_asym");

        Assert.Equal("custom_asym", options.AsymmetricEncryptionCode);
    }

    [Fact]
    public void UseAsymmetricSignature_SetsCode()
    {
        var options = IAMOptions.Create(_configuration, Mock.Of<ISecurityConfigurationProvider>());

        options.UseAsymmetricSignature("custom_sig");

        Assert.Equal("custom_sig", options.AsymmetricSignatureCode);
    }

    [Fact]
    public void UseHash_SetsCode()
    {
        var options = IAMOptions.Create(_configuration, Mock.Of<ISecurityConfigurationProvider>());

        options.UseHash("custom_hash");

        Assert.Equal("custom_hash", options.HashCode);
    }

    [Fact]
    public void RegisterResourceType_WithCaseVariant_OverwritesExisting()
    {
        var options = IAMOptions.Create(_configuration, Mock.Of<ISecurityConfigurationProvider>());

        options.RegisterResourceType("invoice", "Original");
        options.RegisterResourceType("Invoice", "Updated");

        Assert.Single(options.CustomResourceTypes);
        Assert.Equal("Updated", options.CustomResourceTypes["invoice"]);
    }

    [Fact]
    public void FluentChaining_ReturnsSameInstance()
    {
        var options = IAMOptions.Create(_configuration, Mock.Of<ISecurityConfigurationProvider>());

        var result = options
            .RegisterResourceType("invoice", "Invoices")
            .UseSymmetricEncryption("sym")
            .UseAsymmetricEncryption("asym")
            .UseAsymmetricSignature("sig")
            .UseHash("hash");

        Assert.Same(options, result);
    }
}
