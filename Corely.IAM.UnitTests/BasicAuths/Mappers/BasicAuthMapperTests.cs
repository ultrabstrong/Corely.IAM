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
        var request = new CreateBasicAuthRequest(UserId: Guid.CreateVersion7(), Password: "mypassword");

        var result = request.ToBasicAuth(_hashProviderFactory);

        Assert.NotNull(result);
        Assert.Equal(request.UserId, result.UserId);
        Assert.NotNull(result.Password);
        Assert.True(result.Password.Verify("mypassword"));
    }

    [Fact]
    public void ToBasicAuth_FromUpdateRequest_ShouldMapAllProperties()
    {
        var request = new UpdateBasicAuthRequest(UserId: Guid.CreateVersion7(), Password: "mypassword");

        var result = request.ToBasicAuth(_hashProviderFactory);

        Assert.NotNull(result);
        Assert.Equal(request.UserId, result.UserId);
        Assert.NotNull(result.Password);
        Assert.True(result.Password.Verify("mypassword"));
    }

    [Fact]
    public void ToBasicAuth_ShouldHashPassword()
    {
        var request = new CreateBasicAuthRequest(UserId: Guid.CreateVersion7(), Password: "plainpassword");

        var result = request.ToBasicAuth(_hashProviderFactory);

        Assert.NotEqual("plainpassword", result.Password.Hash);
        Assert.True(result.Password.Verify("plainpassword"));
        Assert.False(result.Password.Verify("wrongpassword"));
    }

    [Fact]
    public void ToEntity_ShouldMapAllProperties()
    {
        var request = new CreateBasicAuthRequest(UserId: Guid.CreateVersion7(), Password: "password");
        var basicAuth = request.ToBasicAuth(_hashProviderFactory);
        var modifiedUtc = DateTime.UtcNow;

        var expected = new BasicAuth
        {
            Id = Guid.CreateVersion7(),
            UserId = basicAuth.UserId,
            Password = basicAuth.Password,
            ModifiedUtc = modifiedUtc,
        };

        var result = expected.ToEntity();

        Assert.NotNull(result);
        Assert.Equal(expected.Id, result.Id);
        Assert.Equal(expected.UserId, result.UserId);
        Assert.NotNull(result.Password);
        Assert.NotEmpty(result.Password);
        Assert.Equal(modifiedUtc, result.ModifiedUtc);
    }

    [Fact]
    public void ToEntity_ShouldConvertHashedPasswordToString()
    {
        var request = new CreateBasicAuthRequest(UserId: Guid.CreateVersion7(), Password: "password");
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
            Id = Guid.CreateVersion7(),
            UserId = Guid.CreateVersion7(),
            Password = passwordHash,
            CreatedUtc = DateTime.UtcNow.AddDays(-1),
            ModifiedUtc = DateTime.UtcNow,
        };

        var result = entity.ToModel(_hashProviderFactory);

        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(entity.UserId, result.UserId);
        Assert.NotNull(result.Password);
        Assert.Equal(entity.Password, result.Password.Hash);
        Assert.Equal(entity.ModifiedUtc, result.ModifiedUtc);
    }
}
