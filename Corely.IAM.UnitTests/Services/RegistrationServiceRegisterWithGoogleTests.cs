using AutoFixture;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.GoogleAuths.Processors;
using Corely.IAM.GoogleAuths.Providers;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Invitations.Processors;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Services;

public class RegistrationServiceRegisterWithGoogleTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly Fixture _fixture = new();
    private readonly Mock<IUnitOfWorkProvider> _unitOfWorkProviderMock = new();
    private readonly Mock<IGoogleIdTokenValidator> _googleIdTokenValidatorMock = new();
    private readonly Mock<IGoogleAuthProcessor> _googleAuthProcessorMock = new();
    private readonly Mock<IUserProcessor> _userProcessorMock = new();
    private readonly RegistrationService _registrationService;

    public RegistrationServiceRegisterWithGoogleTests()
    {
        _registrationService = new RegistrationService(
            _serviceFactory.GetRequiredService<ILogger<RegistrationService>>(),
            new Mock<IAccountProcessor>().Object,
            _userProcessorMock.Object,
            new Mock<IBasicAuthProcessor>().Object,
            _googleAuthProcessorMock.Object,
            _googleIdTokenValidatorMock.Object,
            new Mock<IGroupProcessor>().Object,
            new Mock<IRoleProcessor>().Object,
            new Mock<IPermissionProcessor>().Object,
            new Mock<IInvitationProcessor>().Object,
            new Mock<IUserContextProvider>().Object,
            new Mock<IUserContextSetter>().Object,
            _unitOfWorkProviderMock.Object
        );
    }

    [Fact]
    public async Task RegisterUserWithGoogleAsync_ReturnsSuccess_WithValidGoogleToken()
    {
        var payload = new GoogleIdTokenPayload("google-sub-123", "test@gmail.com", true);
        _googleIdTokenValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(payload);

        _googleAuthProcessorMock
            .Setup(x => x.GetUserIdByGoogleSubjectAsync(payload.Subject))
            .ReturnsAsync((Guid?)null);

        var createdUserId = Guid.CreateVersion7();
        _userProcessorMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserRequest>()))
            .ReturnsAsync(
                new CreateUserResult(CreateUserResultCode.Success, string.Empty, createdUserId)
            );

        _googleAuthProcessorMock
            .Setup(x => x.LinkGoogleAuthAsync(createdUserId, It.IsAny<string>()))
            .ReturnsAsync(new LinkGoogleAuthResult(LinkGoogleAuthResultCode.Success, string.Empty));

        var result = await _registrationService.RegisterUserWithGoogleAsync(
            new RegisterUserWithGoogleRequest("valid-token")
        );

        Assert.Equal(RegisterUserWithGoogleResultCode.Success, result.ResultCode);
        Assert.Equal(createdUserId, result.CreatedUserId);
        _unitOfWorkProviderMock.Verify(
            m => m.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task RegisterUserWithGoogleAsync_ReturnsInvalidGoogleTokenError_WhenTokenInvalid()
    {
        _googleIdTokenValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync((GoogleIdTokenPayload?)null);

        var result = await _registrationService.RegisterUserWithGoogleAsync(
            new RegisterUserWithGoogleRequest("invalid-token")
        );

        Assert.Equal(RegisterUserWithGoogleResultCode.InvalidGoogleTokenError, result.ResultCode);
        Assert.Equal(Guid.Empty, result.CreatedUserId);
    }

    [Fact]
    public async Task RegisterUserWithGoogleAsync_ReturnsGoogleAccountInUseError_WhenSubjectAlreadyLinked()
    {
        var payload = new GoogleIdTokenPayload("google-sub-existing", "existing@gmail.com", true);
        _googleIdTokenValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(payload);

        _googleAuthProcessorMock
            .Setup(x => x.GetUserIdByGoogleSubjectAsync(payload.Subject))
            .ReturnsAsync(Guid.CreateVersion7());

        var result = await _registrationService.RegisterUserWithGoogleAsync(
            new RegisterUserWithGoogleRequest("valid-token")
        );

        Assert.Equal(RegisterUserWithGoogleResultCode.GoogleAccountInUseError, result.ResultCode);
        Assert.Equal(Guid.Empty, result.CreatedUserId);
    }

    [Fact]
    public async Task RegisterUserWithGoogleAsync_GeneratesUniqueUsername_WhenPrefixCollides()
    {
        var payload = new GoogleIdTokenPayload("google-sub-new", "testuser@gmail.com", true);
        _googleIdTokenValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(payload);

        _googleAuthProcessorMock
            .Setup(x => x.GetUserIdByGoogleSubjectAsync(payload.Subject))
            .ReturnsAsync((Guid?)null);

        var userId = Guid.CreateVersion7();
        _userProcessorMock
            .SetupSequence(p => p.CreateUserAsync(It.IsAny<CreateUserRequest>()))
            .ReturnsAsync(
                new CreateUserResult(CreateUserResultCode.UserExistsError, "exists", Guid.Empty)
            )
            .ReturnsAsync(new CreateUserResult(CreateUserResultCode.Success, "", userId));

        _googleAuthProcessorMock
            .Setup(x => x.LinkGoogleAuthAsync(userId, It.IsAny<string>()))
            .ReturnsAsync(new LinkGoogleAuthResult(LinkGoogleAuthResultCode.Success, string.Empty));

        var result = await _registrationService.RegisterUserWithGoogleAsync(
            new RegisterUserWithGoogleRequest("valid-token")
        );

        Assert.Equal(RegisterUserWithGoogleResultCode.Success, result.ResultCode);
        Assert.Equal(userId, result.CreatedUserId);
        _userProcessorMock.Verify(
            p => p.CreateUserAsync(It.IsAny<CreateUserRequest>()),
            Times.AtLeast(2)
        );
    }

    [Fact]
    public async Task RegisterUserWithGoogleAsync_ReturnsUserExistsError_WhenEmailAlreadyExists()
    {
        var payload = new GoogleIdTokenPayload("google-sub-new", "taken@gmail.com", true);
        _googleIdTokenValidatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(payload);

        _googleAuthProcessorMock
            .Setup(x => x.GetUserIdByGoogleSubjectAsync(payload.Subject))
            .ReturnsAsync((Guid?)null);

        _userProcessorMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserRequest>()))
            .ReturnsAsync(
                new CreateUserResult(
                    CreateUserResultCode.UserExistsError,
                    $"Email {payload.Email} already exists.",
                    Guid.Empty
                )
            );

        var result = await _registrationService.RegisterUserWithGoogleAsync(
            new RegisterUserWithGoogleRequest("valid-token")
        );

        Assert.Equal(RegisterUserWithGoogleResultCode.UserExistsError, result.ResultCode);
        Assert.Equal(Guid.Empty, result.CreatedUserId);
        _unitOfWorkProviderMock.Verify(
            m => m.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
