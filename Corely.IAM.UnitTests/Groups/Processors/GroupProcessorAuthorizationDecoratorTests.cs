using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Exceptions;
using Corely.IAM.Security.Processors;

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
        var request = new CreateGroupRequest("TestGroup", 1);
        var expectedResult = new CreateGroupResult(CreateGroupResultCode.Success, "", 1);
        _mockInnerProcessor.Setup(x => x.CreateGroupAsync(request)).ReturnsAsync(expectedResult);

        var result = await _decorator.CreateGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.AuthorizeAsync(PermissionConstants.GROUP_RESOURCE_TYPE, AuthAction.Create, null),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.CreateGroupAsync(request), Times.Once);
    }

    [Fact]
    public async Task CreateGroupAsync_ThrowsAuthorizationException_WhenNotAuthorized()
    {
        var request = new CreateGroupRequest("TestGroup", 1);
        _mockAuthorizationProvider
            .Setup(x =>
                x.AuthorizeAsync(PermissionConstants.GROUP_RESOURCE_TYPE, AuthAction.Create, null)
            )
            .ThrowsAsync(
                new AuthorizationException(
                    PermissionConstants.GROUP_RESOURCE_TYPE,
                    AuthAction.Create.ToString()
                )
            );

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _decorator.CreateGroupAsync(request)
        );

        _mockInnerProcessor.Verify(
            x => x.CreateGroupAsync(It.IsAny<CreateGroupRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AddUsersToGroupAsync_CallsAuthorizationProviderWithResourceId()
    {
        var request = new AddUsersToGroupRequest([1, 2], 5);
        var expectedResult = new AddUsersToGroupResult(
            AddUsersToGroupResultCode.Success,
            "",
            2,
            []
        );
        _mockInnerProcessor
            .Setup(x => x.AddUsersToGroupAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.AddUsersToGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.AuthorizeAsync(PermissionConstants.GROUP_RESOURCE_TYPE, AuthAction.Update, 5),
            Times.Once
        );
    }

    [Fact]
    public async Task AddUsersToGroupAsync_ThrowsAuthorizationException_WhenNotAuthorized()
    {
        var request = new AddUsersToGroupRequest([1, 2], 5);
        _mockAuthorizationProvider
            .Setup(x =>
                x.AuthorizeAsync(PermissionConstants.GROUP_RESOURCE_TYPE, AuthAction.Update, 5)
            )
            .ThrowsAsync(
                new AuthorizationException(
                    PermissionConstants.GROUP_RESOURCE_TYPE,
                    AuthAction.Update.ToString(),
                    5
                )
            );

        var exception = await Assert.ThrowsAsync<AuthorizationException>(() =>
            _decorator.AddUsersToGroupAsync(request)
        );

        Assert.Equal(5, exception.ResourceId);
        _mockInnerProcessor.Verify(
            x => x.AddUsersToGroupAsync(It.IsAny<AddUsersToGroupRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_CallsAuthorizationProviderWithResourceId()
    {
        var request = new AssignRolesToGroupRequest([1, 2], 5);
        var expectedResult = new AssignRolesToGroupResult(
            AssignRolesToGroupResultCode.Success,
            "",
            2,
            []
        );
        _mockInnerProcessor
            .Setup(x => x.AssignRolesToGroupAsync(request))
            .ReturnsAsync(expectedResult);

        var result = await _decorator.AssignRolesToGroupAsync(request);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x => x.AuthorizeAsync(PermissionConstants.GROUP_RESOURCE_TYPE, AuthAction.Update, 5),
            Times.Once
        );
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_ThrowsAuthorizationException_WhenNotAuthorized()
    {
        var request = new AssignRolesToGroupRequest([1, 2], 5);
        _mockAuthorizationProvider
            .Setup(x =>
                x.AuthorizeAsync(PermissionConstants.GROUP_RESOURCE_TYPE, AuthAction.Update, 5)
            )
            .ThrowsAsync(
                new AuthorizationException(
                    PermissionConstants.GROUP_RESOURCE_TYPE,
                    AuthAction.Update.ToString(),
                    5
                )
            );

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _decorator.AssignRolesToGroupAsync(request)
        );

        _mockInnerProcessor.Verify(
            x => x.AssignRolesToGroupAsync(It.IsAny<AssignRolesToGroupRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task DeleteGroupAsync_CallsAuthorizationProvider()
    {
        var groupId = 5;
        var expectedResult = new DeleteGroupResult(DeleteGroupResultCode.Success, "");
        _mockInnerProcessor.Setup(x => x.DeleteGroupAsync(groupId)).ReturnsAsync(expectedResult);

        var result = await _decorator.DeleteGroupAsync(groupId);

        Assert.Equal(expectedResult, result);
        _mockAuthorizationProvider.Verify(
            x =>
                x.AuthorizeAsync(
                    PermissionConstants.GROUP_RESOURCE_TYPE,
                    AuthAction.Delete,
                    groupId
                ),
            Times.Once
        );
        _mockInnerProcessor.Verify(x => x.DeleteGroupAsync(groupId), Times.Once);
    }

    [Fact]
    public async Task DeleteGroupAsync_ThrowsAuthorizationException_WhenNotAuthorized()
    {
        var groupId = 5;
        _mockAuthorizationProvider
            .Setup(x =>
                x.AuthorizeAsync(
                    PermissionConstants.GROUP_RESOURCE_TYPE,
                    AuthAction.Delete,
                    groupId
                )
            )
            .ThrowsAsync(
                new AuthorizationException(
                    PermissionConstants.GROUP_RESOURCE_TYPE,
                    AuthAction.Delete.ToString(),
                    groupId
                )
            );

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            _decorator.DeleteGroupAsync(groupId)
        );

        _mockInnerProcessor.Verify(x => x.DeleteGroupAsync(It.IsAny<int>()), Times.Never);
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
