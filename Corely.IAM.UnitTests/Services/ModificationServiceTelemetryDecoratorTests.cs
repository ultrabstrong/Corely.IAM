using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Services;

public class ModificationServiceTelemetryDecoratorTests
{
    private readonly Mock<IModificationService> _mockInnerService;
    private readonly Mock<ILogger<ModificationServiceTelemetryDecorator>> _mockLogger;
    private readonly ModificationServiceTelemetryDecorator _decorator;

    public ModificationServiceTelemetryDecoratorTests()
    {
        _mockInnerService = new Mock<IModificationService>();
        _mockLogger = new Mock<ILogger<ModificationServiceTelemetryDecorator>>();
        _decorator = new ModificationServiceTelemetryDecorator(
            _mockInnerService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task ModifyAccountAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new UpdateAccountRequest(Guid.CreateVersion7(), "TestAccount");
        var expectedResult = new ModifyResult(ModifyResultCode.Success, string.Empty);
        _mockInnerService.Setup(x => x.ModifyAccountAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.ModifyAccountAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ModifyAccountAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task ModifyUserAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new UpdateUserRequest(Guid.CreateVersion7(), "testuser", "test@test.com");
        var expectedResult = new ModifyResult(ModifyResultCode.Success, string.Empty);
        _mockInnerService.Setup(x => x.ModifyUserAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.ModifyUserAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ModifyUserAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task ModifyGroupAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new UpdateGroupRequest(Guid.CreateVersion7(), "TestGroup", "Description");
        var expectedResult = new ModifyResult(ModifyResultCode.Success, string.Empty);
        _mockInnerService.Setup(x => x.ModifyGroupAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.ModifyGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ModifyGroupAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public async Task ModifyRoleAsync_DelegatesToInnerAndLogsResult()
    {
        var request = new UpdateRoleRequest(Guid.CreateVersion7(), "TestRole", "Description");
        var expectedResult = new ModifyResult(ModifyResultCode.Success, string.Empty);
        _mockInnerService.Setup(x => x.ModifyRoleAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.ModifyRoleAsync(request);

        Assert.Equal(expectedResult, result);
        _mockInnerService.Verify(x => x.ModifyRoleAsync(request), Times.Once);
        VerifyLoggedWithResult();
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerService() =>
        Assert.Throws<ArgumentNullException>(() =>
            new ModificationServiceTelemetryDecorator(null!, _mockLogger.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullLogger() =>
        Assert.Throws<ArgumentNullException>(() =>
            new ModificationServiceTelemetryDecorator(_mockInnerService.Object, null!)
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
