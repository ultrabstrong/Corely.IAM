using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Corely.Security.Encryption.Factories;
using Corely.Security.Encryption.Providers;

namespace Corely.IAM.UnitTests.Services;

public class RetrievalServiceTests
{
    private const string TEST_DEVICE_ID = "test-device";

    private readonly Mock<IPermissionProcessor> _mockPermissionProcessor = new();
    private readonly Mock<IGroupProcessor> _mockGroupProcessor = new();
    private readonly Mock<IRoleProcessor> _mockRoleProcessor = new();
    private readonly Mock<IUserProcessor> _mockUserProcessor = new();
    private readonly Mock<IAccountProcessor> _mockAccountProcessor = new();
    private readonly Mock<ISecurityProvider> _mockSecurityProvider = new();
    private readonly Mock<ISymmetricEncryptionProviderFactory> _mockSymmetricEncryptionProviderFactory =
        new();
    private readonly Mock<IUserContextProvider> _mockUserContextProvider = new();
    private readonly RetrievalService _service;
    private readonly UserContext _userContext;

    public RetrievalServiceTests()
    {
        var currentAccountId = Guid.CreateVersion7();
        _userContext = new UserContext(
            new User()
            {
                Id = Guid.CreateVersion7(),
                Username = "testuser",
                Email = "test@test.com",
            },
            new Account() { Id = currentAccountId, AccountName = "TestAccount" },
            TEST_DEVICE_ID,
            [new Account() { Id = currentAccountId, AccountName = "TestAccount" }]
        );

        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns(() => _userContext);

        _mockPermissionProcessor
            .Setup(x =>
                x.GetEffectivePermissionsForUserAsync(
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()
                )
            )
            .ReturnsAsync([]);

        _service = new RetrievalService(
            _mockPermissionProcessor.Object,
            _mockGroupProcessor.Object,
            _mockRoleProcessor.Object,
            _mockUserProcessor.Object,
            _mockAccountProcessor.Object,
            _mockSecurityProvider.Object,
            _mockSymmetricEncryptionProviderFactory.Object,
            _mockUserContextProvider.Object
        );
    }

    #region List Methods

    [Fact]
    public async Task ListPermissions_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var permission = new Permission
        {
            Id = Guid.CreateVersion7(),
            ResourceType = "test",
            ResourceId = Guid.Empty,
        };
        var pagedResult = PagedResult<Permission>.Create([permission], 1, 0, 25);
        _mockPermissionProcessor
            .Setup(x =>
                x.ListPermissionsAsync(
                    It.Is<ListPermissionsRequest>(r =>
                        r.Filter == null && r.Order == null && r.Skip == 0 && r.Take == 25
                    )
                )
            )
            .ReturnsAsync(new ListResult<Permission>(RetrieveResultCode.Success, "", pagedResult));

        var result = await _service.ListPermissionsAsync(
            new ListPermissionsRequest(Guid.CreateVersion7())
        );

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items);
        Assert.Equal(permission.Id, result.Data.Items[0].Id);
        _mockPermissionProcessor.Verify(
            x =>
                x.ListPermissionsAsync(
                    It.Is<ListPermissionsRequest>(r =>
                        r.Filter == null && r.Order == null && r.Skip == 0 && r.Take == 25
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ListGroups_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var group = new Group { Id = Guid.CreateVersion7(), Name = "TestGroup" };
        var pagedResult = PagedResult<Group>.Create([group], 1, 0, 25);
        _mockGroupProcessor
            .Setup(x =>
                x.ListGroupsAsync(
                    It.Is<ListGroupsRequest>(r =>
                        r.Filter == null && r.Order == null && r.Skip == 0 && r.Take == 25
                    )
                )
            )
            .ReturnsAsync(new ListResult<Group>(RetrieveResultCode.Success, "", pagedResult));

        var result = await _service.ListGroupsAsync(new ListGroupsRequest(Guid.CreateVersion7()));

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items);
        Assert.Equal(group.Id, result.Data.Items[0].Id);
        _mockGroupProcessor.Verify(
            x =>
                x.ListGroupsAsync(
                    It.Is<ListGroupsRequest>(r =>
                        r.Filter == null && r.Order == null && r.Skip == 0 && r.Take == 25
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ListRoles_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var role = new Role { Id = Guid.CreateVersion7(), Name = "TestRole" };
        var pagedResult = PagedResult<Role>.Create([role], 1, 0, 25);
        _mockRoleProcessor
            .Setup(x =>
                x.ListRolesAsync(
                    It.Is<ListRolesRequest>(r =>
                        r.Filter == null && r.Order == null && r.Skip == 0 && r.Take == 25
                    )
                )
            )
            .ReturnsAsync(new ListResult<Role>(RetrieveResultCode.Success, "", pagedResult));

        var result = await _service.ListRolesAsync(new ListRolesRequest(Guid.CreateVersion7()));

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items);
        Assert.Equal(role.Id, result.Data.Items[0].Id);
        _mockRoleProcessor.Verify(
            x =>
                x.ListRolesAsync(
                    It.Is<ListRolesRequest>(r =>
                        r.Filter == null && r.Order == null && r.Skip == 0 && r.Take == 25
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ListUsers_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Username = "listuser",
            Email = "list@test.com",
        };
        var pagedResult = PagedResult<User>.Create([user], 1, 0, 25);
        _mockUserProcessor
            .Setup(x =>
                x.ListUsersAsync(
                    It.Is<ListUsersRequest>(r =>
                        r.Filter == null && r.Order == null && r.Skip == 0 && r.Take == 25
                    )
                )
            )
            .ReturnsAsync(new ListResult<User>(RetrieveResultCode.Success, "", pagedResult));

        var result = await _service.ListUsersAsync(new ListUsersRequest(Guid.CreateVersion7()));

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items);
        Assert.Equal(user.Id, result.Data.Items[0].Id);
        _mockUserProcessor.Verify(
            x =>
                x.ListUsersAsync(
                    It.Is<ListUsersRequest>(r =>
                        r.Filter == null && r.Order == null && r.Skip == 0 && r.Take == 25
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ListAccounts_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var account = new Account { Id = Guid.CreateVersion7(), AccountName = "ListAccount" };
        var pagedResult = PagedResult<Account>.Create([account], 1, 0, 25);
        _mockAccountProcessor
            .Setup(x =>
                x.ListAccountsAsync(
                    It.Is<ListAccountsRequest>(r =>
                        r.Filter == null && r.Order == null && r.Skip == 0 && r.Take == 25
                    )
                )
            )
            .ReturnsAsync(new ListResult<Account>(RetrieveResultCode.Success, "", pagedResult));

        var result = await _service.ListAccountsAsync(new ListAccountsRequest());

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items);
        Assert.Equal(account.Id, result.Data.Items[0].Id);
        _mockAccountProcessor.Verify(
            x =>
                x.ListAccountsAsync(
                    It.Is<ListAccountsRequest>(r =>
                        r.Filter == null && r.Order == null && r.Skip == 0 && r.Take == 25
                    )
                ),
            Times.Once
        );
    }

    #endregion

    #region Get Methods - Success

    [Fact]
    public async Task GetPermission_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var permissionId = Guid.CreateVersion7();
        var permission = new Permission
        {
            Id = permissionId,
            ResourceType = "test",
            ResourceId = Guid.Empty,
        };
        _mockPermissionProcessor
            .Setup(x => x.GetPermissionByIdAsync(permissionId, false))
            .ReturnsAsync(new GetResult<Permission>(RetrieveResultCode.Success, "", permission));

        var result = await _service.GetPermissionAsync(permissionId);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Item);
        Assert.Equal(permissionId, result.Item.Id);
        Assert.NotNull(result.EffectivePermissions);
        Assert.Empty(result.EffectivePermissions);
        _mockPermissionProcessor.Verify(
            x => x.GetPermissionByIdAsync(permissionId, false),
            Times.Once
        );
    }

    [Fact]
    public async Task GetGroup_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var groupId = Guid.CreateVersion7();
        var group = new Group { Id = groupId, Name = "TestGroup" };
        _mockGroupProcessor
            .Setup(x => x.GetGroupByIdAsync(groupId, false))
            .ReturnsAsync(new GetResult<Group>(RetrieveResultCode.Success, "", group));

        var result = await _service.GetGroupAsync(groupId);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Item);
        Assert.Equal(groupId, result.Item.Id);
        Assert.NotNull(result.EffectivePermissions);
        Assert.Empty(result.EffectivePermissions);
        _mockGroupProcessor.Verify(x => x.GetGroupByIdAsync(groupId, false), Times.Once);
    }

    [Fact]
    public async Task GetRole_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var roleId = Guid.CreateVersion7();
        var role = new Role { Id = roleId, Name = "TestRole" };
        _mockRoleProcessor
            .Setup(x => x.GetRoleByIdAsync(roleId, false))
            .ReturnsAsync(new GetResult<Role>(RetrieveResultCode.Success, "", role));

        var result = await _service.GetRoleAsync(roleId);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Item);
        Assert.Equal(roleId, result.Item.Id);
        Assert.NotNull(result.EffectivePermissions);
        Assert.Empty(result.EffectivePermissions);
        _mockRoleProcessor.Verify(x => x.GetRoleByIdAsync(roleId, false), Times.Once);
    }

    [Fact]
    public async Task GetUser_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var userId = Guid.CreateVersion7();
        var user = new User
        {
            Id = userId,
            Username = "getuser",
            Email = "get@test.com",
        };
        _mockUserProcessor
            .Setup(x => x.GetUserByIdAsync(userId, false))
            .ReturnsAsync(new GetResult<User>(RetrieveResultCode.Success, "", user));

        var result = await _service.GetUserAsync(userId);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Item);
        Assert.Equal(userId, result.Item.Id);
        Assert.NotNull(result.EffectivePermissions);
        Assert.Empty(result.EffectivePermissions);
        _mockUserProcessor.Verify(x => x.GetUserByIdAsync(userId, false), Times.Once);
    }

    [Fact]
    public async Task GetAccount_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var accountId = Guid.CreateVersion7();
        var account = new Account { Id = accountId, AccountName = "GetAccount" };
        _mockAccountProcessor
            .Setup(x => x.GetAccountByIdAsync(accountId, false))
            .ReturnsAsync(new GetResult<Account>(RetrieveResultCode.Success, "", account));

        var result = await _service.GetAccountAsync(accountId);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Item);
        Assert.Equal(accountId, result.Item.Id);
        Assert.NotNull(result.EffectivePermissions);
        Assert.Empty(result.EffectivePermissions);
        _mockAccountProcessor.Verify(x => x.GetAccountByIdAsync(accountId, false), Times.Once);
    }

    #endregion

    #region Get Methods - Not Found

    [Fact]
    public async Task GetPermission_ReturnsNotFound_WhenProcessorReturnsNotFound()
    {
        var permissionId = Guid.CreateVersion7();
        _mockPermissionProcessor
            .Setup(x => x.GetPermissionByIdAsync(permissionId, false))
            .ReturnsAsync(
                new GetResult<Permission>(RetrieveResultCode.NotFoundError, "Not found", null)
            );

        var result = await _service.GetPermissionAsync(permissionId);

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Item);
        _mockPermissionProcessor.Verify(
            x => x.GetPermissionByIdAsync(permissionId, false),
            Times.Once
        );
    }

    #endregion

    #region GetAccount Provider Methods - Success

    [Fact]
    public async Task GetAccountSymmetricEncryptionProvider_ReturnsSuccess_WhenKeyFound()
    {
        var accountId = _userContext.CurrentAccount!.Id;
        var accountEntity = CreateAccountEntityWithKeys(accountId);
        SetupAccountKeysSuccess(accountId, accountEntity);
        SetupSymmetricEncryptionFactory();
        var mockIamProvider = new Mock<IIamSymmetricEncryptionProvider>();
        _mockSecurityProvider
            .Setup(x => x.BuildSymmetricEncryptionProvider(It.IsAny<SymmetricKey>()))
            .Returns(mockIamProvider.Object);

        var result = await _service.GetAccountSymmetricEncryptionProviderAsync(accountId);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Item);
        _mockAccountProcessor.Verify(x => x.GetAccountKeysAsync(accountId), Times.Once);
        _mockSecurityProvider.Verify(
            x => x.BuildSymmetricEncryptionProvider(It.IsAny<SymmetricKey>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetAccountAsymmetricEncryptionProvider_ReturnsSuccess_WhenKeyFound()
    {
        var accountId = _userContext.CurrentAccount!.Id;
        var accountEntity = CreateAccountEntityWithKeys(accountId);
        SetupAccountKeysSuccess(accountId, accountEntity);
        SetupSymmetricEncryptionFactory();
        var mockIamProvider = new Mock<IIamAsymmetricEncryptionProvider>();
        _mockSecurityProvider
            .Setup(x => x.BuildAsymmetricEncryptionProvider(It.IsAny<AsymmetricKey>()))
            .Returns(mockIamProvider.Object);

        var result = await _service.GetAccountAsymmetricEncryptionProviderAsync(accountId);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Item);
        _mockAccountProcessor.Verify(x => x.GetAccountKeysAsync(accountId), Times.Once);
        _mockSecurityProvider.Verify(
            x => x.BuildAsymmetricEncryptionProvider(It.IsAny<AsymmetricKey>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetAccountAsymmetricSignatureProvider_ReturnsSuccess_WhenKeyFound()
    {
        var accountId = _userContext.CurrentAccount!.Id;
        var accountEntity = CreateAccountEntityWithKeys(accountId);
        SetupAccountKeysSuccess(accountId, accountEntity);
        SetupSymmetricEncryptionFactory();
        var mockIamProvider = new Mock<IIamAsymmetricSignatureProvider>();
        _mockSecurityProvider
            .Setup(x => x.BuildAsymmetricSignatureProvider(It.IsAny<AsymmetricKey>()))
            .Returns(mockIamProvider.Object);

        var result = await _service.GetAccountAsymmetricSignatureProviderAsync(accountId);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Item);
        _mockAccountProcessor.Verify(x => x.GetAccountKeysAsync(accountId), Times.Once);
        _mockSecurityProvider.Verify(
            x => x.BuildAsymmetricSignatureProvider(It.IsAny<AsymmetricKey>()),
            Times.Once
        );
    }

    #endregion

    #region GetAccount Provider Methods - Account Not Found

    [Fact]
    public async Task GetAccountSymmetricEncryptionProvider_ReturnsNotFound_WhenAccountNotFound()
    {
        var accountId = Guid.CreateVersion7();
        _mockAccountProcessor
            .Setup(x => x.GetAccountKeysAsync(accountId))
            .ReturnsAsync(
                new GetResult<AccountEntity>(RetrieveResultCode.NotFoundError, "Not found", null)
            );

        var result = await _service.GetAccountSymmetricEncryptionProviderAsync(accountId);

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Item);
    }

    [Fact]
    public async Task GetAccountAsymmetricEncryptionProvider_ReturnsNotFound_WhenAccountNotFound()
    {
        var accountId = Guid.CreateVersion7();
        _mockAccountProcessor
            .Setup(x => x.GetAccountKeysAsync(accountId))
            .ReturnsAsync(
                new GetResult<AccountEntity>(RetrieveResultCode.NotFoundError, "Not found", null)
            );

        var result = await _service.GetAccountAsymmetricEncryptionProviderAsync(accountId);

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Item);
    }

    [Fact]
    public async Task GetAccountAsymmetricSignatureProvider_ReturnsNotFound_WhenAccountNotFound()
    {
        var accountId = Guid.CreateVersion7();
        _mockAccountProcessor
            .Setup(x => x.GetAccountKeysAsync(accountId))
            .ReturnsAsync(
                new GetResult<AccountEntity>(RetrieveResultCode.NotFoundError, "Not found", null)
            );

        var result = await _service.GetAccountAsymmetricSignatureProviderAsync(accountId);

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Item);
    }

    #endregion

    #region GetAccount Provider Methods - Key Not Found

    [Fact]
    public async Task GetAccountSymmetricEncryptionProvider_ReturnsNotFound_WhenKeyNotFound()
    {
        var accountId = _userContext.CurrentAccount!.Id;
        var accountEntity = new AccountEntity
        {
            Id = accountId,
            AccountName = "TestAccount",
            SymmetricKeys = [],
            AsymmetricKeys = [],
        };
        SetupAccountKeysSuccess(accountId, accountEntity);

        var result = await _service.GetAccountSymmetricEncryptionProviderAsync(accountId);

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Item);
    }

    [Fact]
    public async Task GetAccountAsymmetricEncryptionProvider_ReturnsNotFound_WhenKeyNotFound()
    {
        var accountId = _userContext.CurrentAccount!.Id;
        var accountEntity = new AccountEntity
        {
            Id = accountId,
            AccountName = "TestAccount",
            SymmetricKeys = [],
            AsymmetricKeys = [],
        };
        SetupAccountKeysSuccess(accountId, accountEntity);

        var result = await _service.GetAccountAsymmetricEncryptionProviderAsync(accountId);

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Item);
    }

    [Fact]
    public async Task GetAccountAsymmetricSignatureProvider_ReturnsNotFound_WhenKeyNotFound()
    {
        var accountId = _userContext.CurrentAccount!.Id;
        var accountEntity = new AccountEntity
        {
            Id = accountId,
            AccountName = "TestAccount",
            SymmetricKeys = [],
            AsymmetricKeys = [],
        };
        SetupAccountKeysSuccess(accountId, accountEntity);

        var result = await _service.GetAccountAsymmetricSignatureProviderAsync(accountId);

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Item);
    }

    #endregion

    #region GetUser Provider Methods - Success

    [Fact]
    public async Task GetUserSymmetricEncryptionProvider_ReturnsSuccess_WhenKeyFound()
    {
        var userEntity = CreateUserEntityWithKeys(_userContext.User.Id);
        SetupUserKeysSuccess(userEntity);
        SetupSymmetricEncryptionFactory();
        var mockIamProvider = new Mock<IIamSymmetricEncryptionProvider>();
        _mockSecurityProvider
            .Setup(x => x.BuildSymmetricEncryptionProvider(It.IsAny<SymmetricKey>()))
            .Returns(mockIamProvider.Object);

        var result = await _service.GetUserSymmetricEncryptionProviderAsync();

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Item);
        _mockUserProcessor.Verify(x => x.GetCurrentUserKeysAsync(), Times.Once);
        _mockSecurityProvider.Verify(
            x => x.BuildSymmetricEncryptionProvider(It.IsAny<SymmetricKey>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetUserAsymmetricEncryptionProvider_ReturnsSuccess_WhenKeyFound()
    {
        var userEntity = CreateUserEntityWithKeys(_userContext.User.Id);
        SetupUserKeysSuccess(userEntity);
        SetupSymmetricEncryptionFactory();
        var mockIamProvider = new Mock<IIamAsymmetricEncryptionProvider>();
        _mockSecurityProvider
            .Setup(x => x.BuildAsymmetricEncryptionProvider(It.IsAny<AsymmetricKey>()))
            .Returns(mockIamProvider.Object);

        var result = await _service.GetUserAsymmetricEncryptionProviderAsync();

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Item);
        _mockUserProcessor.Verify(x => x.GetCurrentUserKeysAsync(), Times.Once);
        _mockSecurityProvider.Verify(
            x => x.BuildAsymmetricEncryptionProvider(It.IsAny<AsymmetricKey>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetUserAsymmetricSignatureProvider_ReturnsSuccess_WhenKeyFound()
    {
        var userEntity = CreateUserEntityWithKeys(_userContext.User.Id);
        SetupUserKeysSuccess(userEntity);
        SetupSymmetricEncryptionFactory();
        var mockIamProvider = new Mock<IIamAsymmetricSignatureProvider>();
        _mockSecurityProvider
            .Setup(x => x.BuildAsymmetricSignatureProvider(It.IsAny<AsymmetricKey>()))
            .Returns(mockIamProvider.Object);

        var result = await _service.GetUserAsymmetricSignatureProviderAsync();

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Item);
        _mockUserProcessor.Verify(x => x.GetCurrentUserKeysAsync(), Times.Once);
        _mockSecurityProvider.Verify(
            x => x.BuildAsymmetricSignatureProvider(It.IsAny<AsymmetricKey>()),
            Times.Once
        );
    }

    #endregion

    #region GetUser Provider Methods - User Not Found

    [Fact]
    public async Task GetUserSymmetricEncryptionProvider_ReturnsNotFound_WhenUserNotFound()
    {
        _mockUserProcessor
            .Setup(x => x.GetCurrentUserKeysAsync())
            .ReturnsAsync(
                new GetResult<UserEntity>(RetrieveResultCode.NotFoundError, "Not found", null)
            );

        var result = await _service.GetUserSymmetricEncryptionProviderAsync();

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Item);
    }

    [Fact]
    public async Task GetUserAsymmetricEncryptionProvider_ReturnsNotFound_WhenUserNotFound()
    {
        _mockUserProcessor
            .Setup(x => x.GetCurrentUserKeysAsync())
            .ReturnsAsync(
                new GetResult<UserEntity>(RetrieveResultCode.NotFoundError, "Not found", null)
            );

        var result = await _service.GetUserAsymmetricEncryptionProviderAsync();

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Item);
    }

    [Fact]
    public async Task GetUserAsymmetricSignatureProvider_ReturnsNotFound_WhenUserNotFound()
    {
        _mockUserProcessor
            .Setup(x => x.GetCurrentUserKeysAsync())
            .ReturnsAsync(
                new GetResult<UserEntity>(RetrieveResultCode.NotFoundError, "Not found", null)
            );

        var result = await _service.GetUserAsymmetricSignatureProviderAsync();

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Item);
    }

    #endregion

    #region GetUser Provider Methods - Key Not Found

    [Fact]
    public async Task GetUserSymmetricEncryptionProvider_ReturnsNotFound_WhenKeyNotFound()
    {
        var userEntity = new UserEntity
        {
            Id = _userContext.User.Id,
            Username = "testuser",
            Email = "test@test.com",
            SymmetricKeys = [],
            AsymmetricKeys = [],
        };
        SetupUserKeysSuccess(userEntity);

        var result = await _service.GetUserSymmetricEncryptionProviderAsync();

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Item);
    }

    [Fact]
    public async Task GetUserAsymmetricEncryptionProvider_ReturnsNotFound_WhenKeyNotFound()
    {
        var userEntity = new UserEntity
        {
            Id = _userContext.User.Id,
            Username = "testuser",
            Email = "test@test.com",
            SymmetricKeys = [],
            AsymmetricKeys = [],
        };
        SetupUserKeysSuccess(userEntity);

        var result = await _service.GetUserAsymmetricEncryptionProviderAsync();

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Item);
    }

    [Fact]
    public async Task GetUserAsymmetricSignatureProvider_ReturnsNotFound_WhenKeyNotFound()
    {
        var userEntity = new UserEntity
        {
            Id = _userContext.User.Id,
            Username = "testuser",
            Email = "test@test.com",
            SymmetricKeys = [],
            AsymmetricKeys = [],
        };
        SetupUserKeysSuccess(userEntity);

        var result = await _service.GetUserAsymmetricSignatureProviderAsync();

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Item);
    }

    #endregion

    #region Constructor Null Arguments

    [Fact]
    public void Constructor_Throws_WithNullPermissionProcessor()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new RetrievalService(
                null!,
                _mockGroupProcessor.Object,
                _mockRoleProcessor.Object,
                _mockUserProcessor.Object,
                _mockAccountProcessor.Object,
                _mockSecurityProvider.Object,
                _mockSymmetricEncryptionProviderFactory.Object,
                _mockUserContextProvider.Object
            )
        );

        Assert.Equal("permissionProcessor", ex.ParamName);
    }

    [Fact]
    public void Constructor_Throws_WithNullGroupProcessor()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new RetrievalService(
                _mockPermissionProcessor.Object,
                null!,
                _mockRoleProcessor.Object,
                _mockUserProcessor.Object,
                _mockAccountProcessor.Object,
                _mockSecurityProvider.Object,
                _mockSymmetricEncryptionProviderFactory.Object,
                _mockUserContextProvider.Object
            )
        );

        Assert.Equal("groupProcessor", ex.ParamName);
    }

    [Fact]
    public void Constructor_Throws_WithNullRoleProcessor()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new RetrievalService(
                _mockPermissionProcessor.Object,
                _mockGroupProcessor.Object,
                null!,
                _mockUserProcessor.Object,
                _mockAccountProcessor.Object,
                _mockSecurityProvider.Object,
                _mockSymmetricEncryptionProviderFactory.Object,
                _mockUserContextProvider.Object
            )
        );

        Assert.Equal("roleProcessor", ex.ParamName);
    }

    [Fact]
    public void Constructor_Throws_WithNullUserProcessor()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new RetrievalService(
                _mockPermissionProcessor.Object,
                _mockGroupProcessor.Object,
                _mockRoleProcessor.Object,
                null!,
                _mockAccountProcessor.Object,
                _mockSecurityProvider.Object,
                _mockSymmetricEncryptionProviderFactory.Object,
                _mockUserContextProvider.Object
            )
        );

        Assert.Equal("userProcessor", ex.ParamName);
    }

    [Fact]
    public void Constructor_Throws_WithNullAccountProcessor()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new RetrievalService(
                _mockPermissionProcessor.Object,
                _mockGroupProcessor.Object,
                _mockRoleProcessor.Object,
                _mockUserProcessor.Object,
                null!,
                _mockSecurityProvider.Object,
                _mockSymmetricEncryptionProviderFactory.Object,
                _mockUserContextProvider.Object
            )
        );

        Assert.Equal("accountProcessor", ex.ParamName);
    }

    [Fact]
    public void Constructor_Throws_WithNullSecurityProvider()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new RetrievalService(
                _mockPermissionProcessor.Object,
                _mockGroupProcessor.Object,
                _mockRoleProcessor.Object,
                _mockUserProcessor.Object,
                _mockAccountProcessor.Object,
                null!,
                _mockSymmetricEncryptionProviderFactory.Object,
                _mockUserContextProvider.Object
            )
        );

        Assert.Equal("securityProvider", ex.ParamName);
    }

    [Fact]
    public void Constructor_Throws_WithNullSymmetricEncryptionProviderFactory()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new RetrievalService(
                _mockPermissionProcessor.Object,
                _mockGroupProcessor.Object,
                _mockRoleProcessor.Object,
                _mockUserProcessor.Object,
                _mockAccountProcessor.Object,
                _mockSecurityProvider.Object,
                null!,
                _mockUserContextProvider.Object
            )
        );

        Assert.Equal("symmetricEncryptionProviderFactory", ex.ParamName);
    }

    [Fact]
    public void Constructor_Throws_WithNullUserContextProvider()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new RetrievalService(
                _mockPermissionProcessor.Object,
                _mockGroupProcessor.Object,
                _mockRoleProcessor.Object,
                _mockUserProcessor.Object,
                _mockAccountProcessor.Object,
                _mockSecurityProvider.Object,
                _mockSymmetricEncryptionProviderFactory.Object,
                null!
            )
        );

        Assert.Equal("userContextProvider", ex.ParamName);
    }

    #endregion

    #region Helpers

    private static AccountEntity CreateAccountEntityWithKeys(Guid accountId) =>
        new()
        {
            Id = accountId,
            AccountName = "TestAccount",
            SymmetricKeys =
            [
                new AccountSymmetricKeyEntity
                {
                    Id = Guid.CreateVersion7(),
                    AccountId = accountId,
                    KeyUsedFor = KeyUsedFor.Encryption,
                    ProviderName = "00",
                    Version = 0,
                    EncryptedKey = "00:0:testkey",
                    CreatedUtc = DateTime.UtcNow,
                },
            ],
            AsymmetricKeys =
            [
                new AccountAsymmetricKeyEntity
                {
                    Id = Guid.CreateVersion7(),
                    AccountId = accountId,
                    KeyUsedFor = KeyUsedFor.Encryption,
                    ProviderName = "00",
                    Version = 0,
                    PublicKey = "public-key",
                    EncryptedPrivateKey = "00:0:private-key",
                    CreatedUtc = DateTime.UtcNow,
                },
                new AccountAsymmetricKeyEntity
                {
                    Id = Guid.CreateVersion7(),
                    AccountId = accountId,
                    KeyUsedFor = KeyUsedFor.Signature,
                    ProviderName = "00",
                    Version = 0,
                    PublicKey = "sig-public-key",
                    EncryptedPrivateKey = "00:0:sig-private-key",
                    CreatedUtc = DateTime.UtcNow,
                },
            ],
        };

    private void SetupAccountKeysSuccess(Guid accountId, AccountEntity accountEntity) =>
        _mockAccountProcessor
            .Setup(x => x.GetAccountKeysAsync(accountId))
            .ReturnsAsync(
                new GetResult<AccountEntity>(RetrieveResultCode.Success, "", accountEntity)
            );

    private void SetupSymmetricEncryptionFactory()
    {
        var mockEncryptionProvider = new Mock<ISymmetricEncryptionProvider>();
        _mockSymmetricEncryptionProviderFactory
            .Setup(x => x.GetProviderForDecrypting(It.IsAny<string>()))
            .Returns(mockEncryptionProvider.Object);
    }

    private static UserEntity CreateUserEntityWithKeys(Guid userId) =>
        new()
        {
            Id = userId,
            Username = "testuser",
            Email = "test@test.com",
            SymmetricKeys =
            [
                new UserSymmetricKeyEntity
                {
                    Id = Guid.CreateVersion7(),
                    UserId = userId,
                    KeyUsedFor = KeyUsedFor.Encryption,
                    ProviderName = "00",
                    Version = 0,
                    EncryptedKey = "00:0:testkey",
                    CreatedUtc = DateTime.UtcNow,
                },
            ],
            AsymmetricKeys =
            [
                new UserAsymmetricKeyEntity
                {
                    Id = Guid.CreateVersion7(),
                    UserId = userId,
                    KeyUsedFor = KeyUsedFor.Encryption,
                    ProviderName = "00",
                    Version = 0,
                    PublicKey = "public-key",
                    EncryptedPrivateKey = "00:0:private-key",
                    CreatedUtc = DateTime.UtcNow,
                },
                new UserAsymmetricKeyEntity
                {
                    Id = Guid.CreateVersion7(),
                    UserId = userId,
                    KeyUsedFor = KeyUsedFor.Signature,
                    ProviderName = "00",
                    Version = 0,
                    PublicKey = "sig-public-key",
                    EncryptedPrivateKey = "00:0:sig-private-key",
                    CreatedUtc = DateTime.UtcNow,
                },
            ],
        };

    private void SetupUserKeysSuccess(UserEntity userEntity) =>
        _mockUserProcessor
            .Setup(x => x.GetCurrentUserKeysAsync())
            .ReturnsAsync(new GetResult<UserEntity>(RetrieveResultCode.Success, "", userEntity));

    #endregion
}
