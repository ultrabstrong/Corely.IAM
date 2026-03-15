using Corely.IAM.TotpAuths.Providers;

namespace Corely.IAM.UnitTests.TotpAuths.Providers;

public class TotpProviderTests
{
    private readonly Mock<TimeProvider> _timeProviderMock;
    private readonly TotpProvider _totpProvider;

    public TotpProviderTests()
    {
        _timeProviderMock = new Mock<TimeProvider>();
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(DateTimeOffset.UtcNow);
        _totpProvider = new TotpProvider(_timeProviderMock.Object);
    }

    [Fact]
    public void GenerateSecret_Returns20ByteBase32EncodedString()
    {
        var secret = _totpProvider.GenerateSecret();

        Assert.NotNull(secret);
        Assert.NotEmpty(secret);

        // 20 bytes = 160 bits, base32 encodes 5 bits per char => 32 chars
        Assert.Equal(32, secret.Length);

        // Verify all characters are valid base32
        foreach (var c in secret)
        {
            Assert.True(
                (c >= 'A' && c <= 'Z') || (c >= '2' && c <= '7'),
                $"Invalid base32 character: {c}"
            );
        }
    }

    [Fact]
    public void GenerateSecret_ReturnsDifferentValuesEachCall()
    {
        var secret1 = _totpProvider.GenerateSecret();
        var secret2 = _totpProvider.GenerateSecret();

        Assert.NotEqual(secret1, secret2);
    }

    [Fact]
    public void GenerateSetupUri_ProducesValidOtpauthUri()
    {
        var secret = _totpProvider.GenerateSecret();
        var issuer = "TestIssuer";
        var userLabel = "user@example.com";

        var uri = _totpProvider.GenerateSetupUri(secret, issuer, userLabel);

        Assert.StartsWith("otpauth://totp/", uri);
        Assert.Contains($"secret={secret}", uri);
        Assert.Contains("issuer=TestIssuer", uri);
        Assert.Contains("algorithm=SHA1", uri);
        Assert.Contains("digits=6", uri);
        Assert.Contains("period=30", uri);
    }

    [Fact]
    public void GenerateSetupUri_EncodesSpecialCharactersInIssuerAndLabel()
    {
        var secret = _totpProvider.GenerateSecret();
        var issuer = "My App & Co";
        var userLabel = "user name@test.com";

        var uri = _totpProvider.GenerateSetupUri(secret, issuer, userLabel);

        Assert.StartsWith("otpauth://totp/", uri);
        // Special characters should be URI-encoded
        Assert.DoesNotContain("&", uri.Split('?')[0]);
    }

    [Fact]
    public void GenerateCode_Returns6DigitString()
    {
        var secret = _totpProvider.GenerateSecret();

        var code = _totpProvider.GenerateCode(secret);

        Assert.NotNull(code);
        Assert.Equal(6, code.Length);
        Assert.True(int.TryParse(code, out _), "Code should be numeric");
    }

    [Fact]
    public void ValidateCode_AcceptsValidCode()
    {
        var secret = _totpProvider.GenerateSecret();
        var code = _totpProvider.GenerateCode(secret);

        var result = _totpProvider.ValidateCode(secret, code);

        Assert.True(result);
    }

    [Fact]
    public void ValidateCode_RejectsInvalidCode()
    {
        var secret = _totpProvider.GenerateSecret();

        var result = _totpProvider.ValidateCode(secret, "000000");

        // While theoretically possible to collide, statistically near-zero chance
        // Generate the actual code to ensure we're testing with a different one
        var validCode = _totpProvider.GenerateCode(secret);
        if (validCode == "000000")
        {
            result = _totpProvider.ValidateCode(secret, "999999");
        }

        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    [InlineData("1234567")]
    public void ValidateCode_ReturnsFalse_ForInvalidCodeFormats(string? code)
    {
        var secret = _totpProvider.GenerateSecret();

        var result = _totpProvider.ValidateCode(secret, code!);

        Assert.False(result);
    }

    [Fact]
    public void ValidateCode_AcceptsCodeFromAdjacentTimePeriod()
    {
        var secret = _totpProvider.GenerateSecret();

        // Generate code at time T
        var baseTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(baseTime);
        var codeAtT = _totpProvider.GenerateCode(secret);

        // Validate code at time T+30s (one period later) — should still be valid due to tolerance
        var nextPeriod = baseTime.AddSeconds(30);
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(nextPeriod);

        var result = _totpProvider.ValidateCode(secret, codeAtT);

        Assert.True(result);
    }

    [Fact]
    public void ValidateCode_RejectsCodeFromDistantTimePeriod()
    {
        var secret = _totpProvider.GenerateSecret();

        // Generate code at time T
        var baseTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(baseTime);
        var codeAtT = _totpProvider.GenerateCode(secret);

        // Validate code at time T+90s (three periods later) — outside tolerance
        var distantPeriod = baseTime.AddSeconds(90);
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(distantPeriod);

        var result = _totpProvider.ValidateCode(secret, codeAtT);

        Assert.False(result);
    }
}
