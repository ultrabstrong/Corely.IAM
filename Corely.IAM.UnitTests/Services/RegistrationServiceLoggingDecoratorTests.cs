using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Services;

public class RegistrationServiceLoggingDecoratorTests
{
    private readonly Mock<IRegistrationService> _mockInnerService;
    private readonly Mock<ILogger<RegistrationServiceLoggingDecorator>> _mockLogger;
    private readonly RegistrationServiceLoggingDecorator _decorator;

    public RegistrationServiceLoggingDecoratorTests()
    {
        _mockInnerService = new Mock<IRegistrationService>();
        _mockLogger = new Mock<ILogger<RegistrationServiceLoggingDecorator>>();
        _decorator = new RegistrationServiceLoggingDecorator(
            _mockInnerService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task RegisterUserAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new RegisterUserRequest("testuser", "test@example.com", "password123");
        var expectedResult = new RegisterUserResult(
            RegisterUserResultCode.Success,
            string.Empty,
            1
        );
        _mockInnerService.Setup(x => x.RegisterUserAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterUserAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterUserAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task RegisterAccountAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new RegisterAccountRequest("testaccount");
        var expectedResult = new RegisterAccountResult(
            RegisterAccountResultCode.Success,
            string.Empty,
            Guid.NewGuid()
        );
        _mockInnerService.Setup(x => x.RegisterAccountAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterAccountAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task RegisterGroupAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new RegisterGroupRequest("testgroup");
        var expectedResult = new RegisterGroupResult(
            CreateGroupResultCode.Success,
            string.Empty,
            1
        );
        _mockInnerService.Setup(x => x.RegisterGroupAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterGroupAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task RegisterRoleAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new RegisterRoleRequest("testrole");
        var expectedResult = new RegisterRoleResult(CreateRoleResultCode.Success, string.Empty, 1);
        _mockInnerService.Setup(x => x.RegisterRoleAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterRoleAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterRoleAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task RegisterPermissionAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new RegisterPermissionRequest(
            "Resource",
            1,
            true,
            true,
            true,
            true,
            true,
            "Test permission"
        );
        var expectedResult = new RegisterPermissionResult(
            CreatePermissionResultCode.Success,
            string.Empty,
            1
        );
        _mockInnerService
            .Setup(x => x.RegisterPermissionAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterPermissionAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterPermissionAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task RegisterUserWithAccountAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new RegisterUserWithAccountRequest(1);
        var expectedResult = new RegisterUserWithAccountResult(
            RegisterUserWithAccountResultCode.Success,
            string.Empty
        );
        _mockInnerService
            .Setup(x => x.RegisterUserWithAccountAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterUserWithAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterUserWithAccountAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task RegisterUsersWithGroupAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new RegisterUsersWithGroupRequest([1, 2, 3], 1);
        var expectedResult = new RegisterUsersWithGroupResult(
            AddUsersToGroupResultCode.Success,
            string.Empty,
            3,
            []
        );
        _mockInnerService
            .Setup(x => x.RegisterUsersWithGroupAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterUsersWithGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterUsersWithGroupAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task RegisterRolesWithGroupAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new RegisterRolesWithGroupRequest([1, 2], 1);
        var expectedResult = new RegisterRolesWithGroupResult(
            AssignRolesToGroupResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockInnerService
            .Setup(x => x.RegisterRolesWithGroupAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterRolesWithGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterRolesWithGroupAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task RegisterRolesWithUserAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new RegisterRolesWithUserRequest([1, 2], 1);
        var expectedResult = new RegisterRolesWithUserResult(
            AssignRolesToUserResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockInnerService
            .Setup(x => x.RegisterRolesWithUserAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterRolesWithUserAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterRolesWithUserAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task RegisterPermissionsWithRoleAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new RegisterPermissionsWithRoleRequest([1, 2], 1);
        var expectedResult = new RegisterPermissionsWithRoleResult(
            AssignPermissionsToRoleResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockInnerService
            .Setup(x => x.RegisterPermissionsWithRoleAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RegisterPermissionsWithRoleAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.RegisterPermissionsWithRoleAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerService() =>
        Assert.Throws<ArgumentNullException>(() =>
            new RegistrationServiceLoggingDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new RegistrationServiceLoggingDecorator(_mockInnerService.Object, null!)
        );

    private void VerifyLoggedWithResult() =>
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("with result")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
}
