using Corely.IAM.GoogleAuths.Providers;
using Corely.IAM.Security.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corely.IAM.UnitTests.GoogleAuths.Providers;

public class GoogleIdTokenValidatorTests
{
    [Fact]
    public async Task ValidateAsync_WithNoGoogleClientId_ReturnsNull()
    {
        var validator = CreateValidator(googleClientId: null);

        var result = await validator.ValidateAsync("some-token");

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyGoogleClientId_ReturnsNull()
    {
        var validator = CreateValidator(googleClientId: "");

        var result = await validator.ValidateAsync("some-token");

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_WithWhitespaceGoogleClientId_ReturnsNull()
    {
        var validator = CreateValidator(googleClientId: "   ");

        var result = await validator.ValidateAsync("some-token");

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidToken_ReturnsNull()
    {
        var validator = CreateValidator(googleClientId: "test-client-id");

        var result = await validator.ValidateAsync("not-a-valid-jwt");

        Assert.Null(result);
    }

    private static GoogleIdTokenValidator CreateValidator(string? googleClientId)
    {
        var options = Options.Create(new SecurityOptions { GoogleClientId = googleClientId });
        var logger = new Mock<ILogger<GoogleIdTokenValidator>>();
        return new GoogleIdTokenValidator(options, logger.Object);
    }
}
