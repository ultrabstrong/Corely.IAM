using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.UnitTests.Groups.Processors;

public class GroupProcessorAuthorizationDecoratorTests
{
    private readonly Mock<IGroupProcessor> _mockInnerProcessor = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly GroupProcessorAuthorizationDecorator _decorator;

    public GroupProcessorAuthorizationDecoratorTests()
    {
        _decorator = new GroupProcessorAuthorizationDecorator(
            _mockInnerProcessor.Object,
            _mockAuthorizationProvider.Object
        );
    }

    [Fact]
    public async Task CreateGroupAsync_CallsAuthorizationProvider()
    {
        var request = new CreateGroupRequest("TestGroup", Guid.CreateVersion7());
        var expectedResult = new CreateGroupResult(CreateGroupResultCode.Success, "", request.OwnerAccountId);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Create, PermissionConstants.GROUP_RESOURCE_TYPE)
            )
            .ReturnsAsync(true);
        _mockInnerProcessor.Setup(x => x.CreateGroupAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.CreateGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(AuthAction.Create, PermissionConstants.GROUP_RESOURCE_TYPE),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.CreateGroupAsync(request), Times.Once);
    }

    [Fact]
    public async Task CreateGroupAsync_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var request = new CreateGroupRequest("TestGroup", Guid.CreateVersion7());
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Create, PermissionConstants.GROUP_RESOURCE_TYPE)
            )
            .ReturnsAsync(false);

        var result = await _decorator.CreateGroupAsync(request);

        Assert.Equal(CreateGroupResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.CreateGroupAsync(It.IsAny<CreateGroupRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AddUsersToGroupAsync_CallsAuthorizationProviderWithResourceId()
    {
        var request = new AddUsersToGroupRequest([Guid.CreateVersion7(), Guid.CreateVersion7()], Guid.CreateVersion7());
        var expectedResult = new AddUsersToGroupResult(
            AddUsersToGroupResultCode.Success,
            "",
            2,
            []
        );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE, request.GroupId)
            )
            .ReturnsAsync(true);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.USER_RESOURCE_TYPE, request.UserIds[0], request.UserIds[1])
            )
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.AddUsersToGroupAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.AddUsersToGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE, request.GroupId),
            Times.Once
        );
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.USER_RESOURCE_TYPE, request.UserIds[0], request.UserIds[1]),
            Times.Once
        );
    }

    [Fact]
    public async Task AddUsersToGroupAsync_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var request = new AddUsersToGroupRequest([Guid.CreateVersion7(), Guid.CreateVersion7()], Guid.CreateVersion7());
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE, request.GroupId)
            )
            .ReturnsAsync(false);

        var result = await _decorator.AddUsersToGroupAsync(request);

        Assert.Equal(AddUsersToGroupResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.AddUsersToGroupAsync(It.IsAny<AddUsersToGroupRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AddUsersToGroupAsync_ReturnsUnauthorized_WhenNotAuthorizedToReadUsers()
    {
        var request = new AddUsersToGroupRequest([Guid.CreateVersion7(), Guid.CreateVersion7()], Guid.CreateVersion7());
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE, request.GroupId)
            )
            .ReturnsAsync(true);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.USER_RESOURCE_TYPE, request.UserIds[0], request.UserIds[1])
            )
            .ReturnsAsync(false);

        var result = await _decorator.AddUsersToGroupAsync(request);

        Assert.Equal(AddUsersToGroupResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.AddUsersToGroupAsync(It.IsAny<AddUsersToGroupRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RemoveUsersFromGroupAsync_CallsAuthorizationProviderWithResourceId()
    {
        var request = new RemoveUsersFromGroupRequest([Guid.CreateVersion7(), Guid.CreateVersion7()], Guid.CreateVersion7());
        var expectedResult = new RemoveUsersFromGroupResult(
            RemoveUsersFromGroupResultCode.Success,
            "",
            2,
            []
        );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE, request.GroupId)
            )
            .ReturnsAsync(true);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.USER_RESOURCE_TYPE, request.UserIds[0], request.UserIds[1])
            )
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.RemoveUsersFromGroupAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RemoveUsersFromGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE, request.GroupId),
            Times.Once
        );
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.USER_RESOURCE_TYPE, request.UserIds[0], request.UserIds[1]),
            Times.Once
        );
    }

    [Fact]
    public async Task RemoveUsersFromGroupAsync_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var request = new RemoveUsersFromGroupRequest([Guid.CreateVersion7(), Guid.CreateVersion7()], Guid.CreateVersion7());
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE, request.GroupId)
            )
            .ReturnsAsync(false);

        var result = await _decorator.RemoveUsersFromGroupAsync(request);

        Assert.Equal(RemoveUsersFromGroupResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.RemoveUsersFromGroupAsync(It.IsAny<RemoveUsersFromGroupRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RemoveUsersFromGroupAsync_ReturnsUnauthorized_WhenNotAuthorizedToReadUsers()
    {
        var request = new RemoveUsersFromGroupRequest([Guid.CreateVersion7(), Guid.CreateVersion7()], Guid.CreateVersion7());
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE, request.GroupId)
            )
            .ReturnsAsync(true);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.USER_RESOURCE_TYPE, request.UserIds[0], request.UserIds[1])
            )
            .ReturnsAsync(false);

        var result = await _decorator.RemoveUsersFromGroupAsync(request);

        Assert.Equal(RemoveUsersFromGroupResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.RemoveUsersFromGroupAsync(It.IsAny<RemoveUsersFromGroupRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_CallsAuthorizationProviderWithResourceId()
    {
        var request = new AssignRolesToGroupRequest([Guid.CreateVersion7(), Guid.CreateVersion7()], Guid.CreateVersion7());
        var expectedResult = new AssignRolesToGroupResult(
            AssignRolesToGroupResultCode.Success,
            "",
            2,
            []
        );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE, request.GroupId)
            )
            .ReturnsAsync(true);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.ROLE_RESOURCE_TYPE, request.RoleIds[0], request.RoleIds[1])
            )
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.AssignRolesToGroupAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.AssignRolesToGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE, request.GroupId),
            Times.Once
        );
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.ROLE_RESOURCE_TYPE, request.RoleIds[0], request.RoleIds[1]),
            Times.Once
        );
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var request = new AssignRolesToGroupRequest([Guid.CreateVersion7(), Guid.CreateVersion7()], Guid.CreateVersion7());
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE, request.GroupId)
            )
            .ReturnsAsync(false);

        var result = await _decorator.AssignRolesToGroupAsync(request);

        Assert.Equal(AssignRolesToGroupResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.AssignRolesToGroupAsync(It.IsAny<AssignRolesToGroupRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_ReturnsUnauthorized_WhenNotAuthorizedToReadRoles()
    {
        var request = new AssignRolesToGroupRequest([Guid.CreateVersion7(), Guid.CreateVersion7()], Guid.CreateVersion7());
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE, request.GroupId)
            )
            .ReturnsAsync(true);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.ROLE_RESOURCE_TYPE, request.RoleIds[0], request.RoleIds[1])
            )
            .ReturnsAsync(false);

        var result = await _decorator.AssignRolesToGroupAsync(request);

        Assert.Equal(AssignRolesToGroupResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.AssignRolesToGroupAsync(It.IsAny<AssignRolesToGroupRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RemoveRolesFromGroupAsync_CallsAuthorizationProviderWithResourceId()
    {
        var request = new RemoveRolesFromGroupRequest([Guid.CreateVersion7(), Guid.CreateVersion7()], Guid.CreateVersion7());
        var expectedResult = new RemoveRolesFromGroupResult(
            RemoveRolesFromGroupResultCode.Success,
            "",
            2,
            []
        );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE, request.GroupId)
            )
            .ReturnsAsync(true);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.ROLE_RESOURCE_TYPE, request.RoleIds[0], request.RoleIds[1])
            )
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x => x.RemoveRolesFromGroupAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RemoveRolesFromGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE, request.GroupId),
            Times.Once
        );
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.ROLE_RESOURCE_TYPE, request.RoleIds[0], request.RoleIds[1]),
            Times.Once
        );
    }

    [Fact]
    public async Task RemoveRolesFromGroupAsync_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var request = new RemoveRolesFromGroupRequest([Guid.CreateVersion7(), Guid.CreateVersion7()], Guid.CreateVersion7());
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE, request.GroupId)
            )
            .ReturnsAsync(false);

        var result = await _decorator.RemoveRolesFromGroupAsync(request);

        Assert.Equal(RemoveRolesFromGroupResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.RemoveRolesFromGroupAsync(It.IsAny<RemoveRolesFromGroupRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RemoveRolesFromGroupAsync_ReturnsUnauthorized_WhenNotAuthorizedToReadRoles()
    {
        var request = new RemoveRolesFromGroupRequest([Guid.CreateVersion7(), Guid.CreateVersion7()], Guid.CreateVersion7());
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE, request.GroupId)
            )
            .ReturnsAsync(true);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, PermissionConstants.ROLE_RESOURCE_TYPE, request.RoleIds[0], request.RoleIds[1])
            )
            .ReturnsAsync(false);

        var result = await _decorator.RemoveRolesFromGroupAsync(request);

        Assert.Equal(RemoveRolesFromGroupResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.RemoveRolesFromGroupAsync(It.IsAny<RemoveRolesFromGroupRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RemoveRolesFromGroupAsync_BypassesAuthorization_WhenFlagSet()
    {
        var request = new RemoveRolesFromGroupRequest([Guid.CreateVersion7(), Guid.CreateVersion7()], Guid.CreateVersion7(), BypassAuthorization: true);
        var expectedResult = new RemoveRolesFromGroupResult(
            RemoveRolesFromGroupResultCode.Success,
            "",
            2,
            []
        );
        _mockInnerProcessor
            .Setup(x => x.RemoveRolesFromGroupAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.RemoveRolesFromGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<Guid[]>()),
            Times.Never
        );
        _mockInnerProcessor.Verify(x => x.RemoveRolesFromGroupAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeleteGroupAsync_CallsAuthorizationProvider()
    {
        var groupId = Guid.CreateVersion7();
        var expectedResult = new DeleteGroupResult(DeleteGroupResultCode.Success, "");
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Delete,
                    PermissionConstants.GROUP_RESOURCE_TYPE,
                    groupId
                )
            )
            .ReturnsAsync(true);
        _mockInnerProcessor.Setup(x => x.DeleteGroupAsync(groupId)).ReturnsAsync(expectedResult);

        var result = await _decorator.DeleteGroupAsync(groupId);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(
                    AuthAction.Delete,
                    PermissionConstants.GROUP_RESOURCE_TYPE,
                    groupId
                ),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.DeleteGroupAsync(groupId), Times.Once);
    }

    [Fact]
    public async Task DeleteGroupAsync_ReturnsUnauthorized_WhenNotAuthorized()
    {
        var groupId = Guid.CreateVersion7();
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    AuthAction.Delete,
                    PermissionConstants.GROUP_RESOURCE_TYPE,
                    groupId
                )
            )
            .ReturnsAsync(false);

        var result = await _decorator.DeleteGroupAsync(groupId);

        Assert.Equal(DeleteGroupResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(x => x.DeleteGroupAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new GroupProcessorAuthorizationDecorator(null!, _mockAuthorizationProvider.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullAuthorizationProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new GroupProcessorAuthorizationDecorator(_mockInnerProcessor.Object, null!)
        );
}
