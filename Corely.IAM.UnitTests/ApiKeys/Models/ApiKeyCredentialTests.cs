using Corely.IAM.ApiKeys.Entities;
using Corely.IAM.ApiKeys.Models;

namespace Corely.IAM.UnitTests.ApiKeys.Models;

public class ApiKeyCredentialTests
{
    [Fact]
    public void ToString_ThenTryParse_RoundTrips_WithUnderscoresInTheSecret()
    {
        var credential = new ApiKeyCredential(ApiKeyCredential.NewPrefix(), "se_cr-et_value");

        Assert.True(ApiKeyCredential.TryParse(credential.ToString(), out var parsed));
        Assert.Equal(credential, parsed);
    }

    [Fact]
    public void NewPrefix_IsTheFixedLength_FromTheBase32Alphabet()
    {
        var prefix = ApiKeyCredential.NewPrefix();

        Assert.Equal(ApiKeySecretEntity.PREFIX_LENGTH, prefix.Length);
        Assert.Matches("^[a-z2-7]+$", prefix);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ciam")]
    [InlineData("ciam_abcdefgh")]
    [InlineData("ciam_abcdefgh_")]
    [InlineData("other_abcdefgh_secret")]
    [InlineData("ciam_abc_secret")]
    [InlineData("ciam_ABCDEFGH_secret")]
    [InlineData("ciam_abcdefg1_secret")]
    public void TryParse_RefusesMalformedKeys(string? value) =>
        Assert.False(ApiKeyCredential.TryParse(value, out _));
}
