using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.GoogleAuths.Entities;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.GoogleAuths.Processors;
using Corely.IAM.GoogleAuths.Providers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.GoogleAuths.Processors;

public class GoogleAuthProcessorTests
{
    private const string TEST_GOOGLE_ID_TOKEN = "valid-google-id-token";
    private const string TEST_GOOGLE_SUBJECT = "google-subject-123";
    private const string TEST_GOOGLE_EMAIL = "user@gmail.com";

    private readonly ServiceFactory _serviceFactory = new();
    private readonly Mock<IGoogleIdTokenValidator> _googleIdTokenValidatorMock;
    private readonly GoogleAuthProcessor _processor;
    private readonly IRepo<BasicAuthEntity> _basicAuthRepo;

    public GoogleAuthProcessorTests()
    {
        _googleIdTokenValidatorMock = new Mock<IGoogleIdTokenValidator>();
        _googleIdTokenValidatorMock
            .Setup(v => v.ValidateAsync(TEST_GOOGLE_ID_TOKEN))
            .ReturnsAsync(new GoogleIdTokenPayload(TEST_GOOGLE_SUBJECT, TEST_GOOGLE_EMAIL, true));

        _basicAuthRepo = _serviceFactory.GetRequiredService<IRepo<BasicAuthEntity>>();

        _processor = new GoogleAuthProcessor(
            _serviceFactory.GetRequiredService<IRepo<GoogleAuthEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<BasicAuthEntity>>(),
            _googleIdTokenValidatorMock.Object,
            _serviceFactory.GetRequiredService<ILogger<GoogleAuthProcessor>>()
        );
    }

    private async Task CreateBasicAuthAsync(Guid userId)
    {
        await _basicAuthRepo.CreateAsync(
            new BasicAuthEntity
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                Password = "hashed-password",
            }
        );
    }

    [Fact]
    public async Task LinkGoogleAuthAsync_ReturnsSuccess_WhenValidTokenAndNotAlreadyLinked()
    {
        var result = await _processor.LinkGoogleAuthAsync(
            Guid.CreateVersion7(),
            TEST_GOOGLE_ID_TOKEN
        );

        Assert.Equal(LinkGoogleAuthResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task LinkGoogleAuthAsync_ReturnsInvalidGoogleTokenError_WhenTokenIsInvalid()
    {
        _googleIdTokenValidatorMock
            .Setup(v => v.ValidateAsync("invalid-token"))
            .ReturnsAsync((GoogleIdTokenPayload?)null);

        var result = await _processor.LinkGoogleAuthAsync(Guid.CreateVersion7(), "invalid-token");

        Assert.Equal(LinkGoogleAuthResultCode.InvalidGoogleTokenError, result.ResultCode);
    }

    [Fact]
    public async Task LinkGoogleAuthAsync_ReturnsAlreadyLinkedError_WhenUserAlreadyHasGoogleAuth()
    {
        var userId = Guid.CreateVersion7();
        await _processor.LinkGoogleAuthAsync(userId, TEST_GOOGLE_ID_TOKEN);

        _googleIdTokenValidatorMock
            .Setup(v => v.ValidateAsync("another-token"))
            .ReturnsAsync(new GoogleIdTokenPayload("other-subject", "other@gmail.com", true));

        var result = await _processor.LinkGoogleAuthAsync(userId, "another-token");

        Assert.Equal(LinkGoogleAuthResultCode.AlreadyLinkedError, result.ResultCode);
    }

    [Fact]
    public async Task LinkGoogleAuthAsync_ReturnsGoogleAccountInUseError_WhenSubjectAlreadyLinkedToAnotherUser()
    {
        await _processor.LinkGoogleAuthAsync(Guid.CreateVersion7(), TEST_GOOGLE_ID_TOKEN);

        var result = await _processor.LinkGoogleAuthAsync(
            Guid.CreateVersion7(),
            TEST_GOOGLE_ID_TOKEN
        );

        Assert.Equal(LinkGoogleAuthResultCode.GoogleAccountInUseError, result.ResultCode);
    }

    [Fact]
    public async Task UnlinkGoogleAuthAsync_ReturnsSuccess_WhenLinkedAndHasBasicAuth()
    {
        var userId = Guid.CreateVersion7();
        await _processor.LinkGoogleAuthAsync(userId, TEST_GOOGLE_ID_TOKEN);
        await CreateBasicAuthAsync(userId);

        var result = await _processor.UnlinkGoogleAuthAsync(userId);

        Assert.Equal(UnlinkGoogleAuthResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task UnlinkGoogleAuthAsync_ReturnsNotLinkedError_WhenNotLinked()
    {
        var result = await _processor.UnlinkGoogleAuthAsync(Guid.CreateVersion7());

        Assert.Equal(UnlinkGoogleAuthResultCode.NotLinkedError, result.ResultCode);
    }

    [Fact]
    public async Task UnlinkGoogleAuthAsync_ReturnsLastAuthMethodError_WhenNoBasicAuth()
    {
        var userId = Guid.CreateVersion7();
        await _processor.LinkGoogleAuthAsync(userId, TEST_GOOGLE_ID_TOKEN);

        var result = await _processor.UnlinkGoogleAuthAsync(userId);

        Assert.Equal(UnlinkGoogleAuthResultCode.LastAuthMethodError, result.ResultCode);
    }

    [Fact]
    public async Task GetAuthMethodsAsync_ReturnsBothMethods_WhenBothExist()
    {
        var userId = Guid.CreateVersion7();
        await _processor.LinkGoogleAuthAsync(userId, TEST_GOOGLE_ID_TOKEN);
        await CreateBasicAuthAsync(userId);

        var result = await _processor.GetAuthMethodsAsync(userId);

        Assert.Equal(AuthMethodsResultCode.Success, result.ResultCode);
        Assert.True(result.HasBasicAuth);
        Assert.True(result.HasGoogleAuth);
        Assert.Equal(TEST_GOOGLE_EMAIL, result.GoogleEmail);
    }

    [Fact]
    public async Task GetAuthMethodsAsync_ReturnsNoMethods_WhenNoneExist()
    {
        var result = await _processor.GetAuthMethodsAsync(Guid.CreateVersion7());

        Assert.Equal(AuthMethodsResultCode.Success, result.ResultCode);
        Assert.False(result.HasBasicAuth);
        Assert.False(result.HasGoogleAuth);
        Assert.Null(result.GoogleEmail);
    }

    [Fact]
    public async Task GetAuthMethodsAsync_ReturnsOnlyBasicAuth_WhenNoGoogleAuth()
    {
        var userId = Guid.CreateVersion7();
        await CreateBasicAuthAsync(userId);

        var result = await _processor.GetAuthMethodsAsync(userId);

        Assert.True(result.HasBasicAuth);
        Assert.False(result.HasGoogleAuth);
        Assert.Null(result.GoogleEmail);
    }

    [Fact]
    public async Task GetUserIdByGoogleSubjectAsync_ReturnsUserId_WhenEntityExists()
    {
        var userId = Guid.CreateVersion7();
        await _processor.LinkGoogleAuthAsync(userId, TEST_GOOGLE_ID_TOKEN);

        var result = await _processor.GetUserIdByGoogleSubjectAsync(TEST_GOOGLE_SUBJECT);

        Assert.Equal(userId, result);
    }

    [Fact]
    public async Task GetUserIdByGoogleSubjectAsync_ReturnsNull_WhenEntityDoesNotExist()
    {
        var result = await _processor.GetUserIdByGoogleSubjectAsync("nonexistent-subject");

        Assert.Null(result);
    }
}
