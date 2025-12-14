using Corely.IAM.Models;
using Corely.IAM.Services;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Services;

public class DeregistrationServiceLoggingDecoratorTests
{
    private readonly Mock<IDeregistrationService> _mockInnerService;
    private readonly Mock<ILogger<DeregistrationServiceLoggingDecorator>> _mockLogger;
    private readonly DeregistrationServiceLoggingDecorator _decorator;

    public DeregistrationServiceLoggingDecoratorTests()
    {
        _mockInnerService = new Mock<IDeregistrationService>();
        _mockLogger = new Mock<ILogger<DeregistrationServiceLoggingDecorator>>();
        _decorator = new DeregistrationServiceLoggingDecorator(
            _mockInnerService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task DeregisterUserAsync_DelegatesToInnerAndLogsResult()
    {
        var expectedResult = new DeregisterUserResult(
            DeregisterUserResultCode.Success,
            string.Empty
        );
        _mockInnerService.Setup(x => x.DeregisterUserAsync()).ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterUserAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterUserAsync(), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task DeregisterAccountAsync_DelegatesToInnerAndLogsResult()
    {
        var expectedResult = new DeregisterAccountResult(
            DeregisterAccountResultCode.Success,
            string.Empty
        );
        _mockInnerService.Setup(x => x.DeregisterAccountAsync()).ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterAccountAsync();

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterAccountAsync(), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task DeregisterGroupAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new DeregisterGroupRequest(1);
        var expectedResult = new DeregisterGroupResult(
            DeregisterGroupResultCode.Success,
            string.Empty
        );
        _mockInnerService.Setup(x => x.DeregisterGroupAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterGroupAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task DeregisterRoleAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new DeregisterRoleRequest(1);
        var expectedResult = new DeregisterRoleResult(
            DeregisterRoleResultCode.Success,
            string.Empty
        );
        _mockInnerService.Setup(x => x.DeregisterRoleAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterRoleAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterRoleAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task DeregisterPermissionAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new DeregisterPermissionRequest(1);
        var expectedResult = new DeregisterPermissionResult(
            DeregisterPermissionResultCode.Success,
            string.Empty
        );
        _mockInnerService
            .Setup(x => x.DeregisterPermissionAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterPermissionAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterPermissionAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task DeregisterUserFromAccountAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new DeregisterUserFromAccountRequest(1);
        var expectedResult = new DeregisterUserFromAccountResult(
            DeregisterUserFromAccountResultCode.Success,
            string.Empty
        );
        _mockInnerService
            .Setup(x => x.DeregisterUserFromAccountAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterUserFromAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterUserFromAccountAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task DeregisterUsersFromGroupAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new DeregisterUsersFromGroupRequest([1, 2, 3], 1);
        var expectedResult = new DeregisterUsersFromGroupResult(
            DeregisterUsersFromGroupResultCode.Success,
            string.Empty,
            3,
            []
        );
        _mockInnerService
            .Setup(x => x.DeregisterUsersFromGroupAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterUsersFromGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterUsersFromGroupAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task DeregisterRolesFromGroupAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new DeregisterRolesFromGroupRequest([1, 2], 1);
        var expectedResult = new DeregisterRolesFromGroupResult(
            DeregisterRolesFromGroupResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockInnerService
            .Setup(x => x.DeregisterRolesFromGroupAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterRolesFromGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterRolesFromGroupAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task DeregisterRolesFromUserAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new DeregisterRolesFromUserRequest([1, 2], 1);
        var expectedResult = new DeregisterRolesFromUserResult(
            DeregisterRolesFromUserResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockInnerService
            .Setup(x => x.DeregisterRolesFromUserAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterRolesFromUserAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterRolesFromUserAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task DeregisterPermissionsFromRoleAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new DeregisterPermissionsFromRoleRequest([1, 2], 1);
        var expectedResult = new DeregisterPermissionsFromRoleResult(
            DeregisterPermissionsFromRoleResultCode.Success,
            string.Empty,
            2,
            []
        );
        _mockInnerService
            .Setup(x => x.DeregisterPermissionsFromRoleAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.DeregisterPermissionsFromRoleAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.DeregisterPermissionsFromRoleAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerService() =>
        Assert.Throws<ArgumentNullException>(() =>
            new DeregistrationServiceLoggingDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new DeregistrationServiceLoggingDecorator(_mockInnerService.Object, null!)
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
