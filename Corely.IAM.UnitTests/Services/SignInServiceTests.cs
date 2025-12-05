using AutoFixture;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corely.IAM.UnitTests.Services;

public class SignInServiceTests
{
    private const int MAX_LOGIN_ATTEMPTS = 5;

    private readonly ServiceFactory _serviceFactory = new();
    private readonly Fixture _fixture = new();
    private readonly Mock<IUserProcessor> _userProcessorMock;
    private readonly Mock<IBasicAuthProcessor> _basicAuthProcessorMock;
    private readonly SignInService _signInService;

    private readonly User _user;

    public SignInServiceTests()
    {
        _user = _fixture
            .Build<User>()
            .Without(u => u.SymmetricKeys)
            .Without(u => u.AsymmetricKeys)
            .Create();

        _userProcessorMock = GetMockUserProcessor();
        _basicAuthProcessorMock = GetMockBasicAuthProcessor();

        _signInService = new SignInService(
            _serviceFactory.GetRequiredService<ILogger<SignInService>>(),
            _userProcessorMock.Object,
            _basicAuthProcessorMock.Object,
            Options.Create(new SecurityOptions() { MaxLoginAttempts = MAX_LOGIN_ATTEMPTS })
        );
    }

    private Mock<IUserProcessor> GetMockUserProcessor()
    {
        var mock = new Mock<IUserProcessor>();

        mock.Setup(m => m.GetUserAsync(It.IsAny<string>())).ReturnsAsync(() => _user);

        return mock;
    }

    private static Mock<IBasicAuthProcessor> GetMockBasicAuthProcessor()
    {
        var mock = new Mock<IBasicAuthProcessor>();

        mock.Setup(m => m.VerifyBasicAuthAsync(It.IsAny<VerifyBasicAuthRequest>()))
            .ReturnsAsync(true);

        return mock;
    }

    [Fact]
    public async Task SignInAsync_SucceedsAndUpdateSuccessfulLogin_WhenUserExistsAndPasswordIsValid()
    {
        var request = new SignInRequest(_user.Username, _fixture.Create<string>());
        _user.TotalSuccessfulLogins = 0;
        _user.LastSuccessfulLoginUtc = null;
        _user.TotalFailedLogins = 0;
        _user.FailedLoginsSinceLastSuccess = 0;
        _user.LastFailedLoginUtc = null;

        var result = await _signInService.SignInAsync(request);

        Assert.Equal(SignInResultCode.Success, result.ResultCode);

        _userProcessorMock.Verify(
            m => m.UpdateUserAsync(It.Is<User>(u => HasUpdatedSuccessLogins(u))),
            Times.Once
        );
    }

    private static bool HasUpdatedSuccessLogins(User modified)
    {
        Assert.Equal(1, modified.TotalSuccessfulLogins);
        Assert.NotNull(modified.LastSuccessfulLoginUtc);
        Assert.True((DateTime.UtcNow - modified.LastSuccessfulLoginUtc).Value.TotalSeconds < 5);
        Assert.Equal(0, modified.TotalFailedLogins);
        Assert.Equal(0, modified.FailedLoginsSinceLastSuccess);
        Assert.Null(modified.LastFailedLoginUtc);
        return true;
    }

    [Fact]
    public async Task SignInAsync_Fails_WhenUserDoesNotExist()
    {
        var request = _fixture.Create<SignInRequest>();

        _userProcessorMock.Setup(m => m.GetUserAsync(request.Username)).ReturnsAsync((User)null!);

        var result = await _signInService.SignInAsync(request);

        Assert.Equal(SignInResultCode.UserNotFoundError, result.ResultCode);
        Assert.Equal("User not found", result.Message);
        Assert.Equal(string.Empty, result.AuthToken);
    }

    [Fact]
    public async Task SignInAsync_Fails_WhenUserIsLockedOut()
    {
        var request = new SignInRequest(_user.Username, _fixture.Create<string>());
        _user.FailedLoginsSinceLastSuccess = MAX_LOGIN_ATTEMPTS;

        var result = await _signInService.SignInAsync(request);

        Assert.Equal(SignInResultCode.UserLockedError, result.ResultCode);
        Assert.Equal("User is locked out", result.Message);
        Assert.Equal(string.Empty, result.AuthToken);
    }

    [Fact]
    public async Task SignInAsync_FailsAndUpdatedFailedLogins_WhenPasswordIsInvalid()
    {
        var request = new SignInRequest(_user.Username, _fixture.Create<string>());
        _user.TotalSuccessfulLogins = 0;
        _user.LastSuccessfulLoginUtc = null;
        _user.TotalFailedLogins = 0;
        _user.FailedLoginsSinceLastSuccess = 0;
        _user.LastFailedLoginUtc = null;

        _basicAuthProcessorMock
            .Setup(m => m.VerifyBasicAuthAsync(It.IsAny<VerifyBasicAuthRequest>()))
            .ReturnsAsync(false);

        var result = await _signInService.SignInAsync(request);

        Assert.Equal(SignInResultCode.PasswordMismatchError, result.ResultCode);
        Assert.Equal("Invalid password", result.Message);
        Assert.Equal(string.Empty, result.AuthToken);

        _userProcessorMock.Verify(
            m => m.UpdateUserAsync(It.Is<User>(u => HasUpdatedFailedLogins(u))),
            Times.Once
        );
    }

    private static bool HasUpdatedFailedLogins(User modified)
    {
        Assert.Equal(0, modified.TotalSuccessfulLogins);
        Assert.Null(modified.LastSuccessfulLoginUtc);
        Assert.Equal(1, modified.TotalFailedLogins);
        Assert.Equal(1, modified.FailedLoginsSinceLastSuccess);
        Assert.NotNull(modified.LastFailedLoginUtc);
        Assert.True((DateTime.UtcNow - modified.LastFailedLoginUtc).Value.TotalSeconds < 5);
        return true;
    }

    [Fact]
    public async Task SignInAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _signInService.SignInAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }
}
