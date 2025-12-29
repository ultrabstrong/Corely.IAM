using AutoFixture;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
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
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Services;

public class RegistrationServiceTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly Fixture _fixture = new();
    private readonly Mock<IUnitOfWorkProvider> _unitOfWorkProviderMock = new();
    private readonly Mock<IAccountProcessor> _accountProcessorMock;
    private readonly Mock<IUserProcessor> _userProcessorMock;
    private readonly Mock<IBasicAuthProcessor> _basicAuthProcessorMock;
    private readonly Mock<IGroupProcessor> _groupProcessorMock;
    private readonly Mock<IRoleProcessor> _roleProcessorMock;
    private readonly Mock<IPermissionProcessor> _permissionProcessorMock;
    private readonly Mock<IUserContextProvider> _userContextProviderMock = new();
    private readonly RegistrationService _registrationService;

    private CreateAccountResultCode _createAccountResultCode = CreateAccountResultCode.Success;
    private CreateUserResultCode _createUserResultCode = CreateUserResultCode.Success;
    private CreateBasicAuthResultCode _createBasicAuthResultCode =
        CreateBasicAuthResultCode.Success;
    private CreateGroupResultCode _createGroupResultCode = CreateGroupResultCode.Success;
    private CreateRoleResultCode _createRoleResultCode = CreateRoleResultCode.Success;
    private CreatePermissionResultCode _createPermissionResultCode =
        CreatePermissionResultCode.Success;
    private AddUserToAccountResultCode _addUserToAccountResultCode =
        AddUserToAccountResultCode.Success;

    private AddUsersToGroupResult _addUsersToGroupResult = new(
        AddUsersToGroupResultCode.Success,
        string.Empty,
        0
    );
    private AssignRolesToGroupResult _assignRolesToGroupResult = new(
        AssignRolesToGroupResultCode.Success,
        string.Empty,
        0
    );
    private AssignRolesToUserResult _assignRolesToUserResult = new(
        AssignRolesToUserResultCode.Success,
        string.Empty,
        0
    );
    private AssignPermissionsToRoleResult _assignPermissionsToRoleResult = new(
        AssignPermissionsToRoleResultCode.Success,
        string.Empty,
        0
    );

    public RegistrationServiceTests()
    {
        _accountProcessorMock = GetMockAccountProcessor();
        _userProcessorMock = GetMockUserProcessor();
        _basicAuthProcessorMock = GetMockBasicAuthProcessor();
        _groupProcessorMock = GetMockGroupProcessor();
        _roleProcessorMock = GetMockRoleProcessor();
        _permissionProcessorMock = GetMockPermissionProcessor();

        // Setup user context provider to return a valid context with account ID
        _userContextProviderMock
            .Setup(x => x.GetUserContext())
            .Returns(
                new UserContext(
                    new User() { Id = Guid.CreateVersion7() },
                    new Account() { Id = Guid.CreateVersion7() },
                    "test-device",
                    []
                )
            );

        _registrationService = new RegistrationService(
            _serviceFactory.GetRequiredService<ILogger<RegistrationService>>(),
            _accountProcessorMock.Object,
            _userProcessorMock.Object,
            _basicAuthProcessorMock.Object,
            _groupProcessorMock.Object,
            _roleProcessorMock.Object,
            _permissionProcessorMock.Object,
            _userContextProviderMock.Object,
            _unitOfWorkProviderMock.Object
        );
    }

    private Mock<IAccountProcessor> GetMockAccountProcessor()
    {
        var mock = new Mock<IAccountProcessor>();

        mock.Setup(m => m.CreateAccountAsync(It.IsAny<CreateAccountRequest>()))
            .ReturnsAsync(() =>
                new CreateAccountResult(
                    _createAccountResultCode,
                    string.Empty,
                    Guid.CreateVersion7()
                )
            );

        mock.Setup(m => m.AddUserToAccountAsync(It.IsAny<AddUserToAccountRequest>()))
            .ReturnsAsync(() =>
                new AddUserToAccountResult(_addUserToAccountResultCode, string.Empty)
            );

        return mock;
    }

    private Mock<IUserProcessor> GetMockUserProcessor()
    {
        var mock = new Mock<IUserProcessor>();

        mock.Setup(m => m.CreateUserAsync(It.IsAny<CreateUserRequest>()))
            .ReturnsAsync(() =>
                new CreateUserResult(_createUserResultCode, string.Empty, _fixture.Create<Guid>())
            );

        mock.Setup(m => m.AssignRolesToUserAsync(It.IsAny<AssignRolesToUserRequest>()))
            .ReturnsAsync(() => _assignRolesToUserResult);

        return mock;
    }

    private Mock<IBasicAuthProcessor> GetMockBasicAuthProcessor()
    {
        var mock = new Mock<IBasicAuthProcessor>();

        mock.Setup(m => m.CreateBasicAuthAsync(It.IsAny<CreateBasicAuthRequest>()))
            .ReturnsAsync(() =>
                new CreateBasicAuthResult(
                    _createBasicAuthResultCode,
                    string.Empty,
                    Guid.CreateVersion7()
                )
            );

        return mock;
    }

    private Mock<IGroupProcessor> GetMockGroupProcessor()
    {
        var mock = new Mock<IGroupProcessor>();

        mock.Setup(m => m.CreateGroupAsync(It.IsAny<CreateGroupRequest>()))
            .ReturnsAsync(() =>
                new CreateGroupResult(_createGroupResultCode, string.Empty, Guid.CreateVersion7())
            );

        mock.Setup(m => m.AddUsersToGroupAsync(It.IsAny<AddUsersToGroupRequest>()))
            .ReturnsAsync(() => _addUsersToGroupResult);

        mock.Setup(m => m.AssignRolesToGroupAsync(It.IsAny<AssignRolesToGroupRequest>()))
            .ReturnsAsync(() => _assignRolesToGroupResult);

        return mock;
    }

    private Mock<IRoleProcessor> GetMockRoleProcessor()
    {
        var mock = new Mock<IRoleProcessor>();

        mock.Setup(m => m.CreateRoleAsync(It.IsAny<CreateRoleRequest>()))
            .ReturnsAsync(() =>
                new CreateRoleResult(_createRoleResultCode, string.Empty, Guid.CreateVersion7())
            );

        mock.Setup(m => m.CreateDefaultSystemRolesAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() =>
                new CreateDefaultSystemRolesResult(
                    _fixture.Create<Guid>(),
                    _fixture.Create<Guid>(),
                    _fixture.Create<Guid>()
                )
            );

        mock.Setup(m => m.GetRoleAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(() =>
                new GetRoleResult(GetRoleResultCode.Success, string.Empty, _fixture.Create<Role>())
            );

        mock.Setup(m => m.AssignPermissionsToRoleAsync(It.IsAny<AssignPermissionsToRoleRequest>()))
            .ReturnsAsync(() => _assignPermissionsToRoleResult);

        return mock;
    }

    private Mock<IPermissionProcessor> GetMockPermissionProcessor()
    {
        var mock = new Mock<IPermissionProcessor>();

        mock.Setup(m => m.CreatePermissionAsync(It.IsAny<CreatePermissionRequest>()))
            .ReturnsAsync(() =>
                new CreatePermissionResult(
                    _createPermissionResultCode,
                    string.Empty,
                    _fixture.Create<Guid>()
                )
            );

        return mock;
    }

    [Fact]
    public async Task RegisterUserAsync_Succeeds_WhenAllServicesSucceed()
    {
        var request = _fixture.Create<RegisterUserRequest>();

        var result = await _registrationService.RegisterUserAsync(request);

        Assert.Equal(RegisterUserResultCode.Success, result.ResultCode);

        _unitOfWorkProviderMock.Verify(
            m => m.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task RegisterUserAsync_Fails_WhenUserProcessorFails()
    {
        _createUserResultCode = CreateUserResultCode.UserExistsError;
        var request = _fixture.Create<RegisterUserRequest>();

        var result = await _registrationService.RegisterUserAsync(request);

        Assert.Equal(RegisterUserResultCode.UserCreationError, result.ResultCode);
        _basicAuthProcessorMock.Verify(
            m => m.CreateBasicAuthAsync(It.IsAny<CreateBasicAuthRequest>()),
            Times.Never
        );
        _unitOfWorkProviderMock.Verify(
            m => m.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task RegisterUserAsync_Fails_WhenBasicAuthProcessorFails()
    {
        _createBasicAuthResultCode = CreateBasicAuthResultCode.BasicAuthExistsError;
        var request = _fixture.Create<RegisterUserRequest>();

        var result = await _registrationService.RegisterUserAsync(request);

        Assert.Equal(RegisterUserResultCode.BasicAuthCreationError, result.ResultCode);
        _unitOfWorkProviderMock.Verify(
            m => m.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task RegisterUserAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _registrationService.RegisterUserAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task RegisterAccountAsync_Succeeds_WhenAllServicesSucceed()
    {
        var request = _fixture.Create<RegisterAccountRequest>();

        var result = await _registrationService.RegisterAccountAsync(request);

        Assert.Equal(RegisterAccountResultCode.Success, result.ResultCode);
        _roleProcessorMock.Verify(
            m => m.CreateDefaultSystemRolesAsync(It.IsAny<Guid>()),
            Times.Once
        );
        _userProcessorMock.Verify(
            m => m.AssignRolesToUserAsync(It.IsAny<AssignRolesToUserRequest>()),
            Times.Once
        );
        _unitOfWorkProviderMock.Verify(
            m => m.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task RegisterAccountAsync_Fails_WhenAccountProcessorFails()
    {
        _createAccountResultCode = CreateAccountResultCode.AccountExistsError;
        var request = _fixture.Create<RegisterAccountRequest>();

        var result = await _registrationService.RegisterAccountAsync(request);

        Assert.Equal(RegisterAccountResultCode.AccountCreationError, result.ResultCode);
        _unitOfWorkProviderMock.Verify(
            m => m.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task RegisterAccountAsync_Fails_WhenAssignOwnerRoleFails()
    {
        _assignRolesToUserResult = new(
            AssignRolesToUserResultCode.UserNotFoundError,
            string.Empty,
            -1
        );
        var request = _fixture.Create<RegisterAccountRequest>();

        var result = await _registrationService.RegisterAccountAsync(request);

        Assert.Equal(RegisterAccountResultCode.SystemRoleAssignmentError, result.ResultCode);
        _unitOfWorkProviderMock.Verify(
            m => m.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task RegisterAccountAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() =>
            _registrationService.RegisterAccountAsync(null!)
        );

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task RegisterUserWithAccountAsync_Succeeds_WhenAllServicesSucceed()
    {
        var request = _fixture.Create<RegisterUserWithAccountRequest>();

        var result = await _registrationService.RegisterUserWithAccountAsync(request);

        Assert.Equal(RegisterUserWithAccountResultCode.Success, result.ResultCode);
        _accountProcessorMock.Verify(
            m =>
                m.AddUserToAccountAsync(
                    It.Is<AddUserToAccountRequest>(r => r.UserId == request.UserId)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task RegisterUserWithAccountAsync_Fails_WhenUserNotFound()
    {
        _addUserToAccountResultCode = AddUserToAccountResultCode.UserNotFoundError;
        var request = _fixture.Create<RegisterUserWithAccountRequest>();

        var result = await _registrationService.RegisterUserWithAccountAsync(request);

        Assert.Equal(RegisterUserWithAccountResultCode.UserNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task RegisterUserWithAccountAsync_Fails_WhenAccountNotFound()
    {
        _addUserToAccountResultCode = AddUserToAccountResultCode.AccountNotFoundError;
        var request = _fixture.Create<RegisterUserWithAccountRequest>();

        var result = await _registrationService.RegisterUserWithAccountAsync(request);

        Assert.Equal(RegisterUserWithAccountResultCode.AccountNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task RegisterUserWithAccountAsync_Fails_WhenUserAlreadyInAccount()
    {
        _addUserToAccountResultCode = AddUserToAccountResultCode.UserAlreadyInAccountError;
        var request = _fixture.Create<RegisterUserWithAccountRequest>();

        var result = await _registrationService.RegisterUserWithAccountAsync(request);

        Assert.Equal(
            RegisterUserWithAccountResultCode.UserAlreadyInAccountError,
            result.ResultCode
        );
    }

    [Fact]
    public async Task RegisterUserWithAccountAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() =>
            _registrationService.RegisterUserWithAccountAsync(null!)
        );

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task RegisterGroupAsync_Succeeds_WhenAllServicesSucceed()
    {
        var request = _fixture.Create<RegisterGroupRequest>();
        var result = await _registrationService.RegisterGroupAsync(request);
        Assert.Equal(CreateGroupResultCode.Success, result.ResultCode);
    }

    [Theory]
    [InlineData(CreateGroupResultCode.GroupExistsError)]
    [InlineData(CreateGroupResultCode.AccountNotFoundError)]
    public async Task RegisterGroupAsync_Fails_WhenGroupProcessorFails(
        CreateGroupResultCode createGroupResultCode
    )
    {
        _createGroupResultCode = createGroupResultCode;
        var request = _fixture.Create<RegisterGroupRequest>();

        var result = await _registrationService.RegisterGroupAsync(request);

        Assert.Equal(createGroupResultCode, result.ResultCode);
    }

    [Fact]
    public async Task RegisterGroupAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _registrationService.RegisterGroupAsync(null!));
        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task RegisterRoleAsync_Succeeds_WhenAllServicesSucceed()
    {
        var request = _fixture.Create<RegisterRoleRequest>();
        var result = await _registrationService.RegisterRoleAsync(request);
        Assert.Equal(CreateRoleResultCode.Success, result.ResultCode);
    }

    [Theory]
    [InlineData(CreateRoleResultCode.RoleExistsError)]
    [InlineData(CreateRoleResultCode.AccountNotFoundError)]
    public async Task RegisterRoleAsync_Fails_WhenRoleProcessorFails(
        CreateRoleResultCode createRoleResultCode
    )
    {
        _createRoleResultCode = createRoleResultCode;
        var request = _fixture.Create<RegisterRoleRequest>();
        var result = await _registrationService.RegisterRoleAsync(request);
        Assert.Equal(createRoleResultCode, result.ResultCode);
    }

    [Fact]
    public async Task RegisterRoleAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _registrationService.RegisterRoleAsync(null!));
        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task RegisterPermissionAsync_Succeeds_WhenAllServicesSucceed()
    {
        var request = _fixture.Create<RegisterPermissionRequest>();
        var result = await _registrationService.RegisterPermissionAsync(request);
        Assert.Equal(CreatePermissionResultCode.Success, result.ResultCode);
    }

    [Theory]
    [InlineData(CreatePermissionResultCode.PermissionExistsError)]
    [InlineData(CreatePermissionResultCode.AccountNotFoundError)]
    public async Task RegisterPermissionAsync_Fails_WhenPermissionProcessorFails(
        CreatePermissionResultCode createPermissionResultCode
    )
    {
        _createPermissionResultCode = createPermissionResultCode;
        var request = _fixture.Create<RegisterPermissionRequest>();
        var result = await _registrationService.RegisterPermissionAsync(request);
        Assert.Equal(createPermissionResultCode, result.ResultCode);
    }

    [Fact]
    public async Task RegisterPermissionAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() =>
            _registrationService.RegisterPermissionAsync(null!)
        );
        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task RegisterUsersWithGroupAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() =>
            _registrationService.RegisterUsersWithGroupAsync(null!)
        );
        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task RegisterUsersWithGroupAsync_Succeeds_WhenAllServicesSucceed()
    {
        var request = _fixture.Create<RegisterUsersWithGroupRequest>();
        _addUsersToGroupResult = _fixture.Create<AddUsersToGroupResult>() with
        {
            ResultCode = AddUsersToGroupResultCode.Success,
        };

        var result = await _registrationService.RegisterUsersWithGroupAsync(request);

        Assert.Equal(AddUsersToGroupResultCode.Success, result.ResultCode);
        Assert.Equal(_addUsersToGroupResult.Message, result.Message);
        Assert.Equal(_addUsersToGroupResult.AddedUserCount, result.RegisteredUserCount);
        Assert.Equal(_addUsersToGroupResult.InvalidUserIds.Count, result.InvalidUserIds.Count);
    }

    [Fact]
    public async Task RegisterUsersWithGroupAsync_Fails_WhenGroupProcessorFails()
    {
        var request = _fixture.Create<RegisterUsersWithGroupRequest>();
        _addUsersToGroupResult = _fixture.Create<AddUsersToGroupResult>() with
        {
            ResultCode = AddUsersToGroupResultCode.GroupNotFoundError,
        };

        var result = await _registrationService.RegisterUsersWithGroupAsync(request);

        Assert.Equal(AddUsersToGroupResultCode.GroupNotFoundError, result.ResultCode);
        Assert.Equal(_addUsersToGroupResult.Message, result.Message);
        Assert.Equal(_addUsersToGroupResult.AddedUserCount, result.RegisteredUserCount);
        Assert.Equal(_addUsersToGroupResult.InvalidUserIds.Count, result.InvalidUserIds.Count);
    }

    [Fact]
    public async Task RegisterRolesWithGroup_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() =>
            _registrationService.RegisterRolesWithGroupAsync(null!)
        );
        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task RegisterRolesWithGroup_Succeeds_WhenAllServicesSucceed()
    {
        var request = _fixture.Create<RegisterRolesWithGroupRequest>();
        _assignRolesToGroupResult = _fixture.Create<AssignRolesToGroupResult>() with
        {
            ResultCode = AssignRolesToGroupResultCode.Success,
        };

        var result = await _registrationService.RegisterRolesWithGroupAsync(request);

        Assert.Equal(AssignRolesToGroupResultCode.Success, result.ResultCode);
        Assert.Equal(_assignRolesToGroupResult.Message, result.Message);
        Assert.Equal(_assignRolesToGroupResult.AddedRoleCount, result.RegisteredRoleCount);
        Assert.Equal(_assignRolesToGroupResult.InvalidRoleIds.Count, result.InvalidRoleIds.Count);
    }

    [Fact]
    public async Task RegisterRolesWithGroup_Fails_WhenGroupProcessorFails()
    {
        var request = _fixture.Create<RegisterRolesWithGroupRequest>();
        _assignRolesToGroupResult = _fixture.Create<AssignRolesToGroupResult>() with
        {
            ResultCode = AssignRolesToGroupResultCode.GroupNotFoundError,
        };

        var result = await _registrationService.RegisterRolesWithGroupAsync(request);

        Assert.Equal(AssignRolesToGroupResultCode.GroupNotFoundError, result.ResultCode);
        Assert.Equal(_assignRolesToGroupResult.Message, result.Message);
        Assert.Equal(_assignRolesToGroupResult.AddedRoleCount, result.RegisteredRoleCount);
        Assert.Equal(_assignRolesToGroupResult.InvalidRoleIds.Count, result.InvalidRoleIds.Count);
    }

    [Fact]
    public async Task RegisterRolesWithUser_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() =>
            _registrationService.RegisterRolesWithUserAsync(null!)
        );
        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task RegisterRolesWithUser_Succeeds_WhenAllServicesSucceed()
    {
        var request = _fixture.Create<RegisterRolesWithUserRequest>();
        _assignRolesToUserResult = _fixture.Create<AssignRolesToUserResult>() with
        {
            ResultCode = AssignRolesToUserResultCode.Success,
        };

        var result = await _registrationService.RegisterRolesWithUserAsync(request);

        Assert.Equal(AssignRolesToUserResultCode.Success, result.ResultCode);
        Assert.Equal(_assignRolesToUserResult.Message, result.Message);
        Assert.Equal(_assignRolesToUserResult.AddedRoleCount, result.RegisteredRoleCount);
        Assert.Equal(_assignRolesToUserResult.InvalidRoleIds.Count, result.InvalidRoleIds.Count);
    }

    [Fact]
    public async Task RegisterRolesWithUser_Fails_WhenUserProcessorFails()
    {
        var request = _fixture.Create<RegisterRolesWithUserRequest>();
        _assignRolesToUserResult = _fixture.Create<AssignRolesToUserResult>() with
        {
            ResultCode = AssignRolesToUserResultCode.UserNotFoundError,
        };

        var result = await _registrationService.RegisterRolesWithUserAsync(request);

        Assert.Equal(AssignRolesToUserResultCode.UserNotFoundError, result.ResultCode);
        Assert.Equal(_assignRolesToUserResult.Message, result.Message);
        Assert.Equal(_assignRolesToUserResult.AddedRoleCount, result.RegisteredRoleCount);
        Assert.Equal(_assignRolesToUserResult.InvalidRoleIds.Count, result.InvalidRoleIds.Count);
    }

    [Fact]
    public async Task RegisterPermissionsWithRole_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() =>
            _registrationService.RegisterPermissionsWithRoleAsync(null!)
        );
        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task RegisterPermissionsWithRole_Succeeds_WhenAllServicesSucceed()
    {
        var request = _fixture.Create<RegisterPermissionsWithRoleRequest>();
        _assignPermissionsToRoleResult = _fixture.Create<AssignPermissionsToRoleResult>() with
        {
            ResultCode = AssignPermissionsToRoleResultCode.Success,
        };

        var result = await _registrationService.RegisterPermissionsWithRoleAsync(request);

        Assert.Equal(AssignPermissionsToRoleResultCode.Success, result.ResultCode);
        Assert.Equal(_assignPermissionsToRoleResult.Message, result.Message);
        Assert.Equal(
            _assignPermissionsToRoleResult.AddedPermissionCount,
            result.RegisteredPermissionCount
        );
        Assert.Equal(
            _assignPermissionsToRoleResult.InvalidPermissionIds.Count,
            result.InvalidPermissionIds.Count
        );
    }

    [Fact]
    public async Task RegisterPermissionsWithRole_Fails_WhenRoleProcessorFails()
    {
        var request = _fixture.Create<RegisterPermissionsWithRoleRequest>();
        _assignPermissionsToRoleResult = _fixture.Create<AssignPermissionsToRoleResult>() with
        {
            ResultCode = AssignPermissionsToRoleResultCode.RoleNotFoundError,
        };

        var result = await _registrationService.RegisterPermissionsWithRoleAsync(request);

        Assert.Equal(AssignPermissionsToRoleResultCode.RoleNotFoundError, result.ResultCode);
        Assert.Equal(_assignPermissionsToRoleResult.Message, result.Message);
        Assert.Equal(
            _assignPermissionsToRoleResult.AddedPermissionCount,
            result.RegisteredPermissionCount
        );
        Assert.Equal(
            _assignPermissionsToRoleResult.InvalidPermissionIds.Count,
            result.InvalidPermissionIds.Count
        );
    }
}
