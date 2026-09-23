using Corely.IAM.Security.Mappers;
using Corely.Security.Hashing.Factories;
using Corely.Security.Hashing.Models;

namespace Corely.IAM.UnitTests.Security.Mappers;

public class HashedValueMapperTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly IHashProviderFactory _hashProviderFactory;

    public HashedValueMapperTests()
    {
        _hashProviderFactory = _serviceFactory.GetRequiredService<IHashProviderFactory>();
    }

    [Fact]
    public void ToHashString_ShouldReturnHash_WhenSourceIsValid()
    {
        var hashedValue = "test".ToHashedValueFromPlainText(_hashProviderFactory);

        var result = hashedValue.ToHashString();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void ToHashString_ShouldReturnNull_WhenSourceIsNull()
    {
        IHashedValue? hashedValue = null;

        var result = hashedValue.ToHashString();

        Assert.Null(result);
    }

    [Fact]
    public void ToHashedValue_ShouldCreateHashedValue_WhenSourceIsValid()
    {
        var hashString = "test".ToHashedValueFromPlainText(_hashProviderFactory).Hash;

        var result = hashString.ToHashedValue(_hashProviderFactory);

        Assert.NotNull(result);
        Assert.Equal(hashString, result.Hash);
    }

    [Fact]
    public void ToHashedValueFromPlainText_ShouldHashString()
    {
        var plainText = "mypassword";

        var result = plainText.ToHashedValueFromPlainText(_hashProviderFactory);

        Assert.NotNull(result);
        Assert.NotNull(result.Hash);
        Assert.NotEmpty(result.Hash);
        Assert.NotEqual(plainText, result.Hash);
    }

    [Fact]
    public void ToHashedValueFromPlainText_ShouldCreateVerifiableHash()
    {
        var plainText = "mypassword";

        var hashedValue = plainText.ToHashedValueFromPlainText(_hashProviderFactory);

        Assert.True(hashedValue.Verify(plainText));
        Assert.False(hashedValue.Verify("wrongpassword"));
    }

    [Fact]
    public void ToHashString_ToHashedValue_RoundTrip_ShouldPreserveHash()
    {
        var originalHashedValue = "test".ToHashedValueFromPlainText(_hashProviderFactory);
        var originalHash = originalHashedValue.Hash;

        var hashString = originalHashedValue.ToHashString();
        var resultHashedValue = hashString!.ToHashedValue(_hashProviderFactory);

        Assert.Equal(originalHash, resultHashedValue.Hash);
    }

    [Theory]
    [InlineData("password123")]
    [InlineData("test@email.com")]
    [InlineData("ComplexP@ssw0rd!")]
    public void ToHashedValueFromPlainText_ShouldHandleVariousInputs(string plainText)
    {
        var hashedValue = plainText.ToHashedValueFromPlainText(_hashProviderFactory);

        Assert.NotNull(hashedValue);
        Assert.NotNull(hashedValue.Hash);
        Assert.True(hashedValue.Verify(plainText));
    }
}
