using Corely.IAM.DevTools.Commands;

namespace Corely.IAM.DevTools.UnitTests;

public class SymmetricCommandTests
{
    private static bool IsBase64(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && Convert.TryFromBase64String(value, new byte[512], out _);

    [Fact]
    public void SymEncrypt_Create_WritesABase64Key()
    {
        var key = CommandRunner.Run(new SymmetricEncryption(), "--create");

        Assert.True(IsBase64(key), $"expected a Base64 key, got '{key}'");
        Assert.Equal(32, Convert.FromBase64String(key).Length);
    }

    [Fact]
    public void SymEncrypt_ValidatesTheKeyItCreated()
    {
        var key = CommandRunner.Run(new SymmetricEncryption(), "--create");

        var result = CommandRunner.Run(new SymmetricEncryption(), key, "--validate");

        Assert.Contains("valid", result);
        Assert.DoesNotContain("invalid", result);
    }

    [Fact]
    public void SymEncrypt_ReportsMalformedKeyAsInvalid()
    {
        var result = CommandRunner.Run(new SymmetricEncryption(), "not-base64!!", "--validate");

        Assert.Contains("invalid", result);
    }

    [Fact]
    public void SymEncrypt_RoundTripsAValue()
    {
        var key = CommandRunner.Run(new SymmetricEncryption(), "--create");
        var cipher = CommandRunner.Run(new SymmetricEncryption(), key, "-e", "hello world");

        var plain = CommandRunner.Run(new SymmetricEncryption(), key, "-d", cipher);

        Assert.Equal("hello world", plain);
    }

    [Fact]
    public void SymSign_Create_WritesABase64Key()
    {
        var key = CommandRunner.Run(new SymmetricSignature(), "--create");

        Assert.True(IsBase64(key), $"expected a Base64 key, got '{key}'");
        Assert.Equal(32, Convert.FromBase64String(key).Length);
    }

    [Fact]
    public void SymSign_VerifiesItsOwnSignature()
    {
        var key = CommandRunner.Run(new SymmetricSignature(), "--create");
        var signature = CommandRunner.Run(new SymmetricSignature(), key, "payload");

        var result = CommandRunner.Run(new SymmetricSignature(), key, "payload", "-s", signature);

        Assert.Contains("valid", result);
        Assert.DoesNotContain("invalid", result);
    }

    [Fact]
    public void SymSign_RejectsASignatureForADifferentMessage()
    {
        var key = CommandRunner.Run(new SymmetricSignature(), "--create");
        var signature = CommandRunner.Run(new SymmetricSignature(), key, "payload");

        var result = CommandRunner.Run(new SymmetricSignature(), key, "tampered", "-s", signature);

        Assert.Contains("invalid", result);
    }

    [Fact]
    public void SymEncrypt_List_NamesTheDefaultProvider()
    {
        var result = CommandRunner.Run(new SymmetricEncryption(), "--list");

        Assert.Contains("AES-256-CBC-PKCS7", result);
        Assert.Contains("AES-256-GCM", result);
    }
}
