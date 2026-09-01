using Corely.IAM.DevTools.Commands;

namespace Corely.IAM.DevTools.UnitTests;

/// <summary>
/// The asymmetric commands write their keys to a file, which is where a rendering fault does the
/// most damage: a run that reports success can leave a file holding no usable key.
/// </summary>
public sealed class AsymmetricCommandTests : IDisposable
{
    private readonly string _keyFile = Path.Combine(
        Path.GetTempPath(),
        $"corely-devtools-{Guid.CreateVersion7()}.key"
    );

    public AsymmetricCommandTests() => File.WriteAllText(_keyFile, string.Empty);

    public void Dispose() => File.Delete(_keyFile);

    private string[] KeyFileLines() =>
        File.ReadAllLines(_keyFile).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

    [Fact]
    public void AsymEncrypt_Create_WritesTwoBase64KeysToTheFile()
    {
        CommandRunner.Run(new AsymmetricEncryption(), _keyFile, "--create");

        var lines = KeyFileLines();
        Assert.Equal(2, lines.Length);
        Assert.All(
            lines,
            l => Assert.True(Convert.TryFromBase64String(l, new byte[4096], out _), l)
        );
    }

    [Fact]
    public void AsymEncrypt_ValidatesTheKeysItCreated()
    {
        CommandRunner.Run(new AsymmetricEncryption(), _keyFile, "--create");

        var result = CommandRunner.Run(new AsymmetricEncryption(), _keyFile, "--validate");

        Assert.Contains("valid", result);
        Assert.DoesNotContain("invalid", result);
    }

    [Fact]
    public void AsymEncrypt_RoundTripsAValue()
    {
        CommandRunner.Run(new AsymmetricEncryption(), _keyFile, "--create");
        var cipher = CommandRunner.Run(new AsymmetricEncryption(), _keyFile, "-e", "secret");

        var plain = CommandRunner.Run(new AsymmetricEncryption(), _keyFile, "-d", cipher);

        Assert.Equal("secret", plain);
    }

    [Fact]
    public void AsymSign_Create_WritesTwoBase64KeysToTheFile()
    {
        CommandRunner.Run(new AsymmetricSignature(), _keyFile, "--create");

        var lines = KeyFileLines();
        Assert.Equal(2, lines.Length);
        Assert.All(
            lines,
            l => Assert.True(Convert.TryFromBase64String(l, new byte[4096], out _), l)
        );
    }

    [Fact]
    public void AsymSign_VerifiesItsOwnSignature()
    {
        CommandRunner.Run(new AsymmetricSignature(), _keyFile, "--create");
        var signature = CommandRunner.Run(new AsymmetricSignature(), _keyFile, "message");

        var result = CommandRunner.Run(
            new AsymmetricSignature(),
            _keyFile,
            "message",
            "-s",
            signature
        );

        Assert.Contains("valid", result);
        Assert.DoesNotContain("not valid", result);
    }

    [Fact]
    public void AsymSign_RejectsASignatureForADifferentMessage()
    {
        CommandRunner.Run(new AsymmetricSignature(), _keyFile, "--create");
        var signature = CommandRunner.Run(new AsymmetricSignature(), _keyFile, "message");

        var result = CommandRunner.Run(
            new AsymmetricSignature(),
            _keyFile,
            "tampered",
            "-s",
            signature
        );

        Assert.Contains("not valid", result);
    }
}
