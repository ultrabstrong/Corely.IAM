using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.BasicAuths.Mappers;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.Security.Mappers;
using Corely.Security.Hashing.Factories;

namespace Corely.IAM.UnitTests.BasicAuths.Mappers;

public class BasicAuthMapperTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly IHashProviderFactory _hashProviderFactory;

    public BasicAuthMapperTests()
    {
        _hashProviderFactory = _serviceFactory.GetRequiredService<IHashProviderFactory>();
    }

    [Fact]
    public void ToBasicAuth_FromCreateRequest_ShouldMapAllProperties()
    {
        var request = new CreateBasicAuthRequest(UserId: 123, Password: "mypassword");

        var result = request.ToBasicAuth(_hashProviderFactory);

        Assert.NotNull(result);
        Assert.Equal(123, result.UserId);
        Assert.NotNull(result.Password);
        Assert.True(result.Password.Verify("mypassword"));
    }

    [Fact]
    public void ToBasicAuth_FromUpdateRequest_ShouldMapAllProperties()
    {
        var request = new UpdateBasicAuthRequest(UserId: 123, Password: "mypassword");

        var result = request.ToBasicAuth(_hashProviderFactory);

        Assert.NotNull(result);
        Assert.Equal(123, result.UserId);
        Assert.NotNull(result.Password);
        Assert.True(result.Password.Verify("mypassword"));
    }

    [Fact]
    public void ToBasicAuth_ShouldHashPassword()
    {
        var request = new CreateBasicAuthRequest(UserId: 123, Password: "plainpassword");

        var result = request.ToBasicAuth(_hashProviderFactory);

        Assert.NotEqual("plainpassword", result.Password.Hash);
        Assert.True(result.Password.Verify("plainpassword"));
        Assert.False(result.Password.Verify("wrongpassword"));
    }

    [Fact]
    public void ToEntity_ShouldMapAllProperties()
    {
        var request = new CreateBasicAuthRequest(UserId: 123, Password: "password");
        var basicAuth = request.ToBasicAuth(_hashProviderFactory);
        var modifiedUtc = DateTime.UtcNow;

        var result = new BasicAuth
        {
            Id = 42,
            UserId = basicAuth.UserId,
            Password = basicAuth.Password,
            ModifiedUtc = modifiedUtc,
        }.ToEntity();

        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal(123, result.UserId);
        Assert.NotNull(result.Password);
        Assert.NotEmpty(result.Password);
        Assert.Equal(modifiedUtc, result.ModifiedUtc);
    }

    [Fact]
    public void ToEntity_ShouldConvertHashedPasswordToString()
    {
        var request = new CreateBasicAuthRequest(UserId: 123, Password: "password");
        var basicAuth = request.ToBasicAuth(_hashProviderFactory);

        var result = basicAuth.ToEntity();

        Assert.Equal(basicAuth.Password.Hash, result.Password);
    }

    [Fact]
    public void ToModel_ShouldMapAllProperties()
    {
        var passwordHash = "password".ToHashedValueFromPlainText(_hashProviderFactory).Hash;
        var entity = new BasicAuthEntity
        {
            Id = 42,
            UserId = 123,
            Password = passwordHash,
            CreatedUtc = DateTime.UtcNow.AddDays(-1),
            ModifiedUtc = DateTime.UtcNow,
        };

        var result = entity.ToModel(_hashProviderFactory);

        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal(123, result.UserId);
        Assert.NotNull(result.Password);
        Assert.Equal(entity.Password, result.Password.Hash);
        Assert.Equal(entity.ModifiedUtc, result.ModifiedUtc);
    }

    [Fact]
    public void ToModel_ToEntity_RoundTrip_ShouldPreserveData()
    {
        var request = new CreateBasicAuthRequest(UserId: 456, Password: "testpass");
        var originalBasicAuth = request.ToBasicAuth(_hashProviderFactory);
        var modifiedUtc = DateTime.UtcNow;

        var originalWithId = new BasicAuth
        {
            Id = 99,
            UserId = originalBasicAuth.UserId,
            Password = originalBasicAuth.Password,
            ModifiedUtc = modifiedUtc,
        };

        var entity = originalWithId.ToEntity();
        var resultBasicAuth = entity.ToModel(_hashProviderFactory);

        Assert.Equal(originalWithId.Id, resultBasicAuth.Id);
        Assert.Equal(originalWithId.UserId, resultBasicAuth.UserId);
        Assert.Equal(originalWithId.Password.Hash, resultBasicAuth.Password.Hash);
        Assert.Equal(originalWithId.ModifiedUtc, resultBasicAuth.ModifiedUtc);
    }

    [Theory]
    [InlineData(1, "password123")]
    [InlineData(2, "ComplexP@ssw0rd!")]
    [InlineData(999, "simple")]
    public void ToBasicAuth_ShouldHandleVariousInputs(int userId, string password)
    {
        var request = new CreateBasicAuthRequest(UserId: userId, Password: password);

        var result = request.ToBasicAuth(_hashProviderFactory);

        Assert.Equal(userId, result.UserId);
        Assert.True(result.Password.Verify(password));
    }

    [Theory]
    [InlineData(1, "password1")]
    [InlineData(2, "password2")]
    [InlineData(999, "password3")]
    public void ToEntity_ShouldHandleVariousInputs(int userId, string plainPassword)
    {
        var hashedPassword = plainPassword.ToHashedValueFromPlainText(_hashProviderFactory);
        var basicAuth = new BasicAuth
        {
            Id = userId,
            UserId = userId,
            Password = hashedPassword,
        };

        var result = basicAuth.ToEntity();

        Assert.Equal(userId, result.UserId);
        Assert.Equal(hashedPassword.Hash, result.Password);
    }
}
