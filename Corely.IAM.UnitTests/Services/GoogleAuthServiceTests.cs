using Corely.IAM.Accounts.Models;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.GoogleAuths.Processors;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;

namespace Corely.IAM.UnitTests.Services;

public class GoogleAuthServiceTests
{
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Mock<IGoogleAuthProcessor> _googleAuthProcessorMock = new();
    private readonly Mock<IUserContextProvider> _userContextProviderMock = new();
    private readonly GoogleAuthService _service;

    public GoogleAuthServiceTests()
    {
        var serviceFactory = new ServiceFactory();
        _userContextProviderMock
            .Setup(x => x.GetUserContext())
            .Returns(
                new UserContext(
                    new User { Id = _userId, Email = "user@test.com" },
                    new Account { Id = Guid.CreateVersion7() },
                    "device-1",
                    []
                )
            );

        _service = new GoogleAuthService(
            _googleAuthProcessorMock.Object,
            _userContextProviderMock.Object,
            serviceFactory.GetRequiredService<IValidationProvider>()
        );
    }

    [Fact]
    public async Task LinkGoogleAuthAsync_ReturnsInvalidGoogleTokenError_WhenRequestInvalid()
    {
        var result = await _service.LinkGoogleAuthAsync(new LinkGoogleAuthRequest(string.Empty));

        Assert.Equal(LinkGoogleAuthResultCode.InvalidGoogleTokenError, result.ResultCode);
        _googleAuthProcessorMock.Verify(
            x => x.LinkGoogleAuthAsync(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task LinkGoogleAuthAsync_DelegatesToProcessor_WhenRequestValid()
    {
        var expected = new LinkGoogleAuthResult(LinkGoogleAuthResultCode.Success, string.Empty);
        _googleAuthProcessorMock
            .Setup(x => x.LinkGoogleAuthAsync(_userId, "valid-token"))
            .ReturnsAsync(expected);

        var result = await _service.LinkGoogleAuthAsync(new LinkGoogleAuthRequest("valid-token"));

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task UnlinkGoogleAuthAsync_DelegatesToProcessor()
    {
        var expected = new UnlinkGoogleAuthResult(UnlinkGoogleAuthResultCode.Success, string.Empty);
        _googleAuthProcessorMock
            .Setup(x => x.UnlinkGoogleAuthAsync(_userId))
            .ReturnsAsync(expected);

        var result = await _service.UnlinkGoogleAuthAsync();

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetAuthMethodsAsync_DelegatesToProcessor()
    {
        var expected = new AuthMethodsResult(
            AuthMethodsResultCode.Success,
            string.Empty,
            true,
            true,
            "user@test.com"
        );
        _googleAuthProcessorMock.Setup(x => x.GetAuthMethodsAsync(_userId)).ReturnsAsync(expected);

        var result = await _service.GetAuthMethodsAsync();

        Assert.Equal(expected, result);
    }
}
