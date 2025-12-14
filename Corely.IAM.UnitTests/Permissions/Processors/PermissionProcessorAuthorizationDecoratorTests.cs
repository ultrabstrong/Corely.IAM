using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.UnitTests.Permissions.Processors;

public class PermissionProcessorAuthorizationDecoratorTests
{
    private readonly Mock<IPermissionProcessor> _mockInnerProcessor = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly PermissionProcessorAuthorizationDecorator _decorator;

    public PermissionProcessorAuthorizationDecoratorTests()
    {
        _decorator = new PermissionProcessorAuthorizationDecorator(
            _mockInnerProcessor.Object,
            _mockAuthorizationProvider.Object
        );
    }

    [Fact]
    public async Task CreatePermissionAsync_CallsAuthorizationProvider()
    {
        var request = new CreatePermissionRequest(1, "group", 0, Read: true);
        var expectedResult = new CreatePermissionResult(CreatePermissionResultCode.Success, "", 1);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Create,
                    PermissionConstants.PERMISSION_RESOURCE_TYPE,
                    null
                )
            )
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.CreatePermissionAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.CreatePermissionAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(
                    AuthAction.Create,
                    PermissionConstants.PERMISSION_RESOURCE_TYPE,
                    null
                ),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.CreatePermissionAsync(request), Times.Once);
    }

    [Fact]
    public async Task CreatePermissionAsync_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var request = new CreatePermissionRequest(1, "group", 0, Read: true);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Create,
                    PermissionConstants.PERMISSION_RESOURCE_TYPE,
                    null
                )
            )
            .ReturnsAsync(false);

        var result = await _decorator.CreatePermissionAsync(request);

        Assert.Equal(CreatePermissionResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.CreatePermissionAsync(It.IsAny<CreatePermissionRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task CreateDefaultSystemPermissionsAsync_BypassesAuthorization()
    {
        var accountId = 1;

        await _decorator.CreateDefaultSystemPermissionsAsync(accountId);

        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<int?>()),
            Times.Never
        );
        _mockInnerProcessor.Verify(
            x => x.CreateDefaultSystemPermissionsAsync(accountId),
            Times.Once
        );
    }

    [Fact]
    public async Task DeletePermissionAsync_CallsAuthorizationProvider()
    {
        var permissionId = 5;
        var expectedResult = new DeletePermissionResult(DeletePermissionResultCode.Success, "");
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Delete,
                    PermissionConstants.PERMISSION_RESOURCE_TYPE,
                    permissionId
                )
            )
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.DeletePermissionAsync(permissionId))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.DeletePermissionAsync(permissionId);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(
                    AuthAction.Delete,
                    PermissionConstants.PERMISSION_RESOURCE_TYPE,
                    permissionId
                ),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.DeletePermissionAsync(permissionId), Times.Once);
    }

    [Fact]
    public async Task DeletePermissionAsync_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var permissionId = 5;
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Delete,
                    PermissionConstants.PERMISSION_RESOURCE_TYPE,
                    permissionId
                )
            )
            .ReturnsAsync(false);

        var result = await _decorator.DeletePermissionAsync(permissionId);

        Assert.Equal(DeletePermissionResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(x => x.DeletePermissionAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new PermissionProcessorAuthorizationDecorator(null!, _mockAuthorizationProvider.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullAuthorizationProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new PermissionProcessorAuthorizationDecorator(_mockInnerProcessor.Object, null!)
        );
}
