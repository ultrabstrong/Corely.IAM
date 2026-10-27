using Corely.IAM.ApiKeys.Enums;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.ApiKeys.Processors;
using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.UnitTests.ApiKeys.Processors;

public class ApiKeyProcessorAuthorizationDecoratorTests
{
    private readonly Mock<IApiKeyProcessor> _mockInnerProcessor = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly ApiKeyProcessorAuthorizationDecorator _decorator;
    private readonly Guid _accountId = Guid.CreateVersion7();
    private readonly Guid _apiKeyId = Guid.CreateVersion7();

    public ApiKeyProcessorAuthorizationDecoratorTests()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext(It.IsAny<Guid>())).Returns(true);
        _mockAuthorizationProvider
            .Setup(x => x.CanGrantRolesAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(true);
        _mockInnerProcessor
            .Setup(x =>
                x.GetApiKeyByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<AccountScope>())
            )
            .ReturnsAsync(new GetResult<ApiKey>(RetrieveResultCode.NotFoundError, "", null));
        _decorator = new ApiKeyProcessorAuthorizationDecorator(
            _mockInnerProcessor.Object,
            _mockAuthorizationProvider.Object
        );
    }

    private void Allow(AuthAction action, string resourceType) =>
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedAsync(action, resourceType, It.IsAny<Guid[]>()))
            .ReturnsAsync(true);

    [Fact]
    public async Task CreateApiKey_NeedsCreateOnApiKeys()
    {
        var request = new CreateApiKeyRequest("ci", _accountId);
        var expected = new CreateApiKeyResult(
            CreateApiKeyResultCode.Success,
            "",
            _apiKeyId,
            "ciam_abcdefgh_secret"
        );
        Allow(AuthAction.Create, ApiKey.RESOURCE_TYPE);
        _mockInnerProcessor.Setup(x => x.CreateApiKeyAsync(request)).ReturnsAsync(expected);

        Assert.Equal(expected, await _decorator.CreateApiKeyAsync(request));
    }

    [Fact]
    public async Task CreateApiKey_ReturnsUnauthorized_WithoutCreate()
    {
        var result = await _decorator.CreateApiKeyAsync(new CreateApiKeyRequest("ci", _accountId));

        Assert.Equal(CreateApiKeyResultCode.UnauthorizedError, result.ResultCode);
        Assert.Null(result.Key);
        _mockInnerProcessor.Verify(
            x => x.CreateApiKeyAsync(It.IsAny<CreateApiKeyRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task CreateApiKey_ReturnsUnauthorized_OutsideTheAccountContext()
    {
        Allow(AuthAction.Create, ApiKey.RESOURCE_TYPE);
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext(_accountId)).Returns(false);

        var result = await _decorator.CreateApiKeyAsync(new CreateApiKeyRequest("ci", _accountId));

        Assert.Equal(CreateApiKeyResultCode.UnauthorizedError, result.ResultCode);
    }

    [Fact]
    public async Task RotateAndUpdate_NeedUpdateOnTheKey()
    {
        var rotate = new RotateApiKeySecretRequest(_apiKeyId, _accountId, ApiKeySlot.Primary);
        var update = new UpdateApiKeyRequest(_apiKeyId, _accountId, "ci", null);

        var deniedRotate = await _decorator.RotateApiKeySecretAsync(rotate);
        var deniedUpdate = await _decorator.UpdateApiKeyAsync(update);
        Allow(AuthAction.Update, ApiKey.RESOURCE_TYPE);
        await _decorator.RotateApiKeySecretAsync(rotate);
        await _decorator.UpdateApiKeyAsync(update);

        Assert.Equal(RotateApiKeySecretResultCode.UnauthorizedError, deniedRotate.ResultCode);
        Assert.Equal(ModifyResultCode.UnauthorizedError, deniedUpdate.ResultCode);
        _mockInnerProcessor.Verify(x => x.RotateApiKeySecretAsync(rotate), Times.Once);
        _mockInnerProcessor.Verify(x => x.UpdateApiKeyAsync(update), Times.Once);
    }

    [Fact]
    public async Task RotateApiKeySecret_IsRefused_WhenTheKeyHoldsRolesTheCallerCannotGrant()
    {
        var roleId = Guid.CreateVersion7();
        var groupId = Guid.CreateVersion7();
        var request = new RotateApiKeySecretRequest(_apiKeyId, _accountId, ApiKeySlot.Primary);
        Allow(AuthAction.Update, ApiKey.RESOURCE_TYPE);
        _mockInnerProcessor
            .Setup(x => x.GetApiKeyByIdAsync(_apiKeyId, true, It.IsAny<AccountScope>()))
            .ReturnsAsync(
                new GetResult<ApiKey>(
                    RetrieveResultCode.Success,
                    "",
                    new ApiKey
                    {
                        Id = _apiKeyId,
                        Roles = [new ChildRef(roleId, "Admin")],
                        Groups = [new ChildRef(groupId, "Admins")],
                    }
                )
            );
        _mockAuthorizationProvider
            .Setup(x => x.CanGrantRolesAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(roleId))))
            .ReturnsAsync(false);

        var refused = await _decorator.RotateApiKeySecretAsync(request);
        _mockAuthorizationProvider
            .Setup(x => x.CanGrantRolesAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(true);
        _mockAuthorizationProvider.Setup(x => x.CanGrantGroupAsync(groupId)).ReturnsAsync(false);
        var refusedForGroup = await _decorator.RotateApiKeySecretAsync(request);
        _mockAuthorizationProvider.Setup(x => x.CanGrantGroupAsync(groupId)).ReturnsAsync(true);
        await _decorator.RotateApiKeySecretAsync(request);

        Assert.Equal(RotateApiKeySecretResultCode.UnauthorizedError, refused.ResultCode);
        Assert.Equal(RotateApiKeySecretResultCode.UnauthorizedError, refusedForGroup.ResultCode);
        _mockInnerProcessor.Verify(x => x.RotateApiKeySecretAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeleteApiKey_NeedsDeleteOnTheKey()
    {
        var denied = await _decorator.DeleteApiKeyAsync(_apiKeyId, _accountId);
        Allow(AuthAction.Delete, ApiKey.RESOURCE_TYPE);
        await _decorator.DeleteApiKeyAsync(_apiKeyId, _accountId);

        Assert.Equal(DeleteApiKeyResultCode.UnauthorizedError, denied.ResultCode);
        _mockInnerProcessor.Verify(x => x.DeleteApiKeyAsync(_apiKeyId, _accountId), Times.Once);
    }

    [Fact]
    public async Task AssignRolesToApiKey_NeedsUpdateOnTheKey_ReadOnTheRoles_AndTheGrant()
    {
        var request = new AssignRolesToApiKeyRequest(
            [Guid.CreateVersion7()],
            _apiKeyId,
            _accountId
        );
        Allow(AuthAction.Update, ApiKey.RESOURCE_TYPE);
        Allow(AuthAction.Read, Role.RESOURCE_TYPE);

        await _decorator.AssignRolesToApiKeyAsync(request);

        _mockInnerProcessor.Verify(x => x.AssignRolesToApiKeyAsync(request), Times.Once);
    }

    [Fact]
    public async Task AssignRolesToApiKey_ReturnsUnauthorized_WhenTheCallerCannotGrantTheRoles()
    {
        var request = new AssignRolesToApiKeyRequest(
            [Guid.CreateVersion7()],
            _apiKeyId,
            _accountId
        );
        Allow(AuthAction.Update, ApiKey.RESOURCE_TYPE);
        Allow(AuthAction.Read, Role.RESOURCE_TYPE);
        _mockAuthorizationProvider
            .Setup(x => x.CanGrantRolesAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(false);

        var result = await _decorator.AssignRolesToApiKeyAsync(request);

        Assert.Equal(AssignRolesToApiKeyResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.AssignRolesToApiKeyAsync(It.IsAny<AssignRolesToApiKeyRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AssignRolesToApiKey_ReturnsUnauthorized_WithoutReadOnTheRoles()
    {
        Allow(AuthAction.Update, ApiKey.RESOURCE_TYPE);

        var result = await _decorator.AssignRolesToApiKeyAsync(
            new AssignRolesToApiKeyRequest([Guid.CreateVersion7()], _apiKeyId, _accountId)
        );

        Assert.Equal(AssignRolesToApiKeyResultCode.UnauthorizedError, result.ResultCode);
    }

    [Fact]
    public async Task RemoveRolesFromApiKey_NeedsUpdateOnTheKey_ButNoGrant()
    {
        var request = new RemoveRolesFromApiKeyRequest(
            [Guid.CreateVersion7()],
            _apiKeyId,
            _accountId
        );
        var denied = await _decorator.RemoveRolesFromApiKeyAsync(request);
        Allow(AuthAction.Update, ApiKey.RESOURCE_TYPE);
        Allow(AuthAction.Read, Role.RESOURCE_TYPE);
        _mockAuthorizationProvider
            .Setup(x => x.CanGrantRolesAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(false);

        await _decorator.RemoveRolesFromApiKeyAsync(request);

        Assert.Equal(RemoveRolesFromApiKeyResultCode.UnauthorizedError, denied.ResultCode);
        _mockInnerProcessor.Verify(x => x.RemoveRolesFromApiKeyAsync(request), Times.Once);
    }

    [Fact]
    public async Task ListApiKeys_PassesThePermittedIds()
    {
        var request = new ListApiKeysRequest(_accountId);
        IReadOnlySet<Guid> permitted = new HashSet<Guid> { _apiKeyId };
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedAsync(AuthAction.Read, ApiKey.RESOURCE_TYPE))
            .ReturnsAsync(true);
        _mockAuthorizationProvider
            .Setup(x => x.GetAuthorizedResourceIdsAsync(AuthAction.Read, ApiKey.RESOURCE_TYPE))
            .ReturnsAsync(permitted);

        await _decorator.ListApiKeysAsync(request);

        _mockInnerProcessor.Verify(x => x.ListApiKeysAsync(request, permitted), Times.Once);
    }

    [Fact]
    public async Task ListApiKeys_ReturnsUnauthorized_WithoutRead()
    {
        var result = await _decorator.ListApiKeysAsync(new ListApiKeysRequest(_accountId));

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
    }

    [Fact]
    public async Task GetApiKeyById_NeedsReadOnTheKey()
    {
        var denied = await _decorator.GetApiKeyByIdAsync(_apiKeyId, true, _accountId);
        Allow(AuthAction.Read, ApiKey.RESOURCE_TYPE);
        await _decorator.GetApiKeyByIdAsync(_apiKeyId, true, _accountId);

        Assert.Equal(RetrieveResultCode.UnauthorizedError, denied.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.GetApiKeyByIdAsync(_apiKeyId, true, _accountId),
            Times.Once
        );
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new ApiKeyProcessorAuthorizationDecorator(null!, _mockAuthorizationProvider.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullAuthorizationProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new ApiKeyProcessorAuthorizationDecorator(_mockInnerProcessor.Object, null!)
        );
}
