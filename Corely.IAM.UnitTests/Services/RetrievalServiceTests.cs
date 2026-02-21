using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.UnitTests.Services;

public class RetrievalServiceTests
{
    private const string TEST_DEVICE_ID = "test-device";

    private readonly Mock<IPermissionProcessor> _mockPermissionProcessor = new();
    private readonly Mock<IGroupProcessor> _mockGroupProcessor = new();
    private readonly Mock<IRoleProcessor> _mockRoleProcessor = new();
    private readonly Mock<IUserProcessor> _mockUserProcessor = new();
    private readonly Mock<IAccountProcessor> _mockAccountProcessor = new();
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
            _mockUserContextProvider.Object
        );
    }

    #region List Methods

    [Fact]
    public async Task ListPermissionsAsync_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var permission = new Permission
        {
            Id = Guid.CreateVersion7(),
            ResourceType = "test",
            ResourceId = Guid.Empty,
        };
        var pagedResult = PagedResult<Permission>.Create([permission], 1, 0, 25);
        _mockPermissionProcessor
            .Setup(x => x.ListPermissionsAsync(null, null, 0, 25))
            .ReturnsAsync(new ListResult<Permission>(RetrieveResultCode.Success, "", pagedResult));

        var result = await _service.ListPermissionsAsync();

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items);
        Assert.Equal(permission.Id, result.Data.Items[0].Id);
        _mockPermissionProcessor.Verify(x => x.ListPermissionsAsync(null, null, 0, 25), Times.Once);
    }

    [Fact]
    public async Task ListGroupsAsync_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var group = new Group { Id = Guid.CreateVersion7(), Name = "TestGroup" };
        var pagedResult = PagedResult<Group>.Create([group], 1, 0, 25);
        _mockGroupProcessor
            .Setup(x => x.ListGroupsAsync(null, null, 0, 25))
            .ReturnsAsync(new ListResult<Group>(RetrieveResultCode.Success, "", pagedResult));

        var result = await _service.ListGroupsAsync();

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items);
        Assert.Equal(group.Id, result.Data.Items[0].Id);
        _mockGroupProcessor.Verify(x => x.ListGroupsAsync(null, null, 0, 25), Times.Once);
    }

    [Fact]
    public async Task ListRolesAsync_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var role = new Role { Id = Guid.CreateVersion7(), Name = "TestRole" };
        var pagedResult = PagedResult<Role>.Create([role], 1, 0, 25);
        _mockRoleProcessor
            .Setup(x => x.ListRolesAsync(null, null, 0, 25))
            .ReturnsAsync(new ListResult<Role>(RetrieveResultCode.Success, "", pagedResult));

        var result = await _service.ListRolesAsync();

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items);
        Assert.Equal(role.Id, result.Data.Items[0].Id);
        _mockRoleProcessor.Verify(x => x.ListRolesAsync(null, null, 0, 25), Times.Once);
    }

    [Fact]
    public async Task ListUsersAsync_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Username = "listuser",
            Email = "list@test.com",
        };
        var pagedResult = PagedResult<User>.Create([user], 1, 0, 25);
        _mockUserProcessor
            .Setup(x => x.ListUsersAsync(null, null, 0, 25))
            .ReturnsAsync(new ListResult<User>(RetrieveResultCode.Success, "", pagedResult));

        var result = await _service.ListUsersAsync();

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items);
        Assert.Equal(user.Id, result.Data.Items[0].Id);
        _mockUserProcessor.Verify(x => x.ListUsersAsync(null, null, 0, 25), Times.Once);
    }

    [Fact]
    public async Task ListAccountsAsync_ReturnsSuccess_WhenProcessorSucceeds()
    {
        var account = new Account { Id = Guid.CreateVersion7(), AccountName = "ListAccount" };
        var pagedResult = PagedResult<Account>.Create([account], 1, 0, 25);
        _mockAccountProcessor
            .Setup(x => x.ListAccountsAsync(null, null, 0, 25))
            .ReturnsAsync(new ListResult<Account>(RetrieveResultCode.Success, "", pagedResult));

        var result = await _service.ListAccountsAsync();

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items);
        Assert.Equal(account.Id, result.Data.Items[0].Id);
        _mockAccountProcessor.Verify(x => x.ListAccountsAsync(null, null, 0, 25), Times.Once);
    }

    #endregion

    #region Get Methods - Success

    [Fact]
    public async Task GetPermissionAsync_ReturnsSuccess_WhenProcessorSucceeds()
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
    public async Task GetGroupAsync_ReturnsSuccess_WhenProcessorSucceeds()
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
    public async Task GetRoleAsync_ReturnsSuccess_WhenProcessorSucceeds()
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
    public async Task GetUserAsync_ReturnsSuccess_WhenProcessorSucceeds()
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
    public async Task GetAccountAsync_ReturnsSuccess_WhenProcessorSucceeds()
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
    public async Task GetPermissionAsync_ReturnsNotFound_WhenProcessorReturnsNotFound()
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
                _mockUserContextProvider.Object
            )
        );

        Assert.Equal("accountProcessor", ex.ParamName);
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
                null!
            )
        );

        Assert.Equal("userContextProvider", ex.ParamName);
    }

    #endregion
}
