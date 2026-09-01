using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.GoogleAuths.Entities;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.GoogleAuths.Processors;
using Corely.IAM.GoogleAuths.Providers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.GoogleAuths.Processors;

public class GoogleUnverifiedEmailTests
{
    private const string UNVERIFIED_TOKEN = "unverified-google-id-token";
    private const string VERIFIED_TOKEN = "verified-google-id-token";
    private const string SUBJECT = "google-subject-123";
    private const string EMAIL = "user@gmail.com";

    private readonly ServiceFactory _serviceFactory = new();
    private readonly Mock<IGoogleIdTokenValidator> _validatorMock = new();
    private readonly GoogleAuthProcessor _processor;
    private readonly IRepo<BasicAuthEntity> _basicAuthRepo;

    public GoogleUnverifiedEmailTests()
    {
        _validatorMock
            .Setup(v => v.ValidateAsync(UNVERIFIED_TOKEN))
            .ReturnsAsync(new GoogleIdTokenPayload(SUBJECT, EMAIL, false));
        _validatorMock
            .Setup(v => v.ValidateAsync(VERIFIED_TOKEN))
            .ReturnsAsync(new GoogleIdTokenPayload(SUBJECT, EMAIL, true));

        _basicAuthRepo = _serviceFactory.GetRequiredService<IRepo<BasicAuthEntity>>();

        _processor = new GoogleAuthProcessor(
            _serviceFactory.GetRequiredService<IRepo<GoogleAuthEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<BasicAuthEntity>>(),
            _validatorMock.Object,
            _serviceFactory.GetRequiredService<ILogger<GoogleAuthProcessor>>()
        );
    }

    [Fact]
    public async Task LinkingWithAnUnverifiedEmail_IsRefused()
    {
        var userId = await CreateBasicAuthAsync();

        var result = await _processor.LinkGoogleAuthAsync(userId, UNVERIFIED_TOKEN);

        Assert.Equal(LinkGoogleAuthResultCode.EmailNotVerifiedError, result.ResultCode);
    }

    [Fact]
    public async Task LinkingWithAnUnverifiedEmail_StoresNothing()
    {
        var userId = await CreateBasicAuthAsync();

        await _processor.LinkGoogleAuthAsync(userId, UNVERIFIED_TOKEN);

        var googleAuthRepo = _serviceFactory.GetRequiredService<IRepo<GoogleAuthEntity>>();
        Assert.Null(await googleAuthRepo.GetAsync(e => e.UserId == userId));
    }

    [Fact]
    public async Task LinkingWithAVerifiedEmail_Succeeds()
    {
        var userId = await CreateBasicAuthAsync();

        var result = await _processor.LinkGoogleAuthAsync(userId, VERIFIED_TOKEN);

        Assert.Equal(LinkGoogleAuthResultCode.Success, result.ResultCode);
    }

    private async Task<Guid> CreateBasicAuthAsync()
    {
        var userId = Guid.CreateVersion7();
        await _basicAuthRepo.CreateAsync(
            new BasicAuthEntity
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                Password = "hashed",
            }
        );
        return userId;
    }
}
