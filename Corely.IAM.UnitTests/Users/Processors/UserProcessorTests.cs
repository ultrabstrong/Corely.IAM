using System.IdentityModel.Tokens.Jwt;
using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Processors;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Validators;
using Corely.Security.Encryption.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corely.IAM.UnitTests.Users.Processors;

public class UserProcessorTests
{
    private const string VALID_USERNAME = "username";
    private const string VALID_EMAIL = "email@x.y";

    private readonly Fixture _fixture = new();
    private readonly ServiceFactory _serviceFactory = new();
    private readonly UserProcessor _userProcessor;

    public UserProcessorTests()
    {
        _userProcessor = new UserProcessor(
            _serviceFactory.GetRequiredService<IRepo<UserEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<UserAuthTokenEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<RoleEntity>>(),
            _serviceFactory.GetRequiredService<ISecurityProcessor>(),
            _serviceFactory.GetRequiredService<ISymmetricEncryptionProviderFactory>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<IOptions<SecurityOptions>>(),
            _serviceFactory.GetRequiredService<ILogger<UserProcessor>>()
        );
    }

    private async Task<(int UserId, int AccountId)> CreateUserAsync()
    {
        var account = new AccountEntity { Id = _fixture.Create<int>() };
        var user = new UserEntity { Username = _fixture.Create<string>(), Accounts = [account] };
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var created = await userRepo.CreateAsync(user);
        return (created.Id, account.Id);
    }

    private async Task<int> CreateRoleAsync(int accountId, params int[] userIds)
    {
        var roleId = _fixture.Create<int>();
        var role = new RoleEntity
        {
            Id = roleId,
            Users = userIds?.Select(u => new UserEntity { Id = u })?.ToList() ?? [],
            AccountId = accountId,
            Account = new AccountEntity { Id = accountId },
        };
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var created = await roleRepo.CreateAsync(role);
        return created.Id;
    }

    [Fact]
    public async Task CreateUserAsync_Fails_WhenUserExists()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        await _userProcessor.CreateUserAsync(request);

        var result = await _userProcessor.CreateUserAsync(request);

        Assert.Equal(CreateUserResultCode.UserExistsError, result.ResultCode);
    }

    [Fact]
    public async Task CreateUser_ReturnsCreateUserResult()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        var res = await _userProcessor.CreateUserAsync(request);
        Assert.Equal(CreateUserResultCode.Success, res.ResultCode);
    }

    [Fact]
    public async Task CreateUser_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _userProcessor.CreateUserAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task GetUserByUseridAsync_ReturnsNull_WhenUserNotFound()
    {
        var user = await _userProcessor.GetUserAsync(_fixture.Create<int>());
        Assert.Null(user);
    }

    [Fact]
    public async Task GetUserByUseridAsync_ReturnsUser_WhenUserExists()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        var result = await _userProcessor.CreateUserAsync(request);

        var user = await _userProcessor.GetUserAsync(result.CreatedId);

        Assert.NotNull(user);
        Assert.Equal(request.Username, user.Username);
        Assert.Equal(request.Email, user.Email);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_ReturnsNull_WhenUserNotFound()
    {
        var user = await _userProcessor.GetUserAsync(_fixture.Create<string>());
        Assert.Null(user);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_ReturnsUser_WhenUserExists()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        await _userProcessor.CreateUserAsync(request);

        var user = await _userProcessor.GetUserAsync(request.Username);

        Assert.NotNull(user);
        Assert.Equal(request.Username, user.Username);
        Assert.Equal(request.Email, user.Email);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesUser()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        await _userProcessor.CreateUserAsync(request);
        var user = await _userProcessor.GetUserAsync(request.Username);
        user!.Disabled = false;

        await _userProcessor.UpdateUserAsync(user);
        var updatedUser = await _userProcessor.GetUserAsync(request.Username);

        Assert.False(updatedUser!.Disabled);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_ReturnsAuthToken()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        var result = await _userProcessor.CreateUserAsync(request);

        var token = await _userProcessor.GetUserAuthTokenAsync(result.CreatedId);

        Assert.NotNull(token);

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        Assert.Equal(typeof(UserProcessor).FullName, jwtToken.Issuer);
        Assert.Equal("Corely.IAM", jwtToken.Audiences.First());
        Assert.Contains(
            jwtToken.Claims,
            c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == result.CreatedId.ToString()
        );
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Jti);
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Iat);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_ReturnsNull_WhenUserDNE()
    {
        var token = await _userProcessor.GetUserAuthTokenAsync(_fixture.Create<int>());

        Assert.Null(token);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_ReturnsNull_WhenSignatureKeyDNE()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        var result = await _userProcessor.CreateUserAsync(request);

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var user = await userRepo.GetAsync(u => u.Id == result.CreatedId);
        user?.SymmetricKeys?.Clear();
        user?.AsymmetricKeys?.Clear();
        await userRepo.UpdateAsync(user!);

        var token = await _userProcessor.GetUserAuthTokenAsync(result.CreatedId);

        Assert.Null(token);
    }

    [Fact]
    public async Task IsUserAuthTokenValidAsync_ReturnsTrue_WithValidToken()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        var result = await _userProcessor.CreateUserAsync(request);
        var token = await _userProcessor.GetUserAuthTokenAsync(result.CreatedId);

        var isValid = await _userProcessor.IsUserAuthTokenValidAsync(result.CreatedId, token!);

        Assert.True(isValid);
    }

    [Fact]
    public async Task IsUserAuthTokenValidAsync_ReturnsFalse_WithInvalidTokenFormat()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        var result = await _userProcessor.CreateUserAsync(request);
        var token = await _userProcessor.GetUserAuthTokenAsync(result.CreatedId);

        var isValid = await _userProcessor.IsUserAuthTokenValidAsync(
            result.CreatedId,
            token! + "invalid"
        );

        Assert.False(isValid);
    }

    [Fact]
    public async Task IsUserAuthTokenValidAsync_ReturnsFalse_WhenUserDNE()
    {
        var isValid = await _userProcessor.IsUserAuthTokenValidAsync(
            _fixture.Create<int>(),
            _fixture.Create<string>()
        );

        Assert.False(isValid);
    }

    [Fact]
    public async Task IsUserAuthTokenValidAsync_ReturnsFalse_WhenSignatureKeyDNE()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        var result = await _userProcessor.CreateUserAsync(request);
        var token = await _userProcessor.GetUserAuthTokenAsync(result.CreatedId);

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var user = await userRepo.GetAsync(u => u.Id == result.CreatedId);
        user?.SymmetricKeys?.Clear();
        user?.AsymmetricKeys?.Clear();
        await userRepo.UpdateAsync(user!);

        var isValid = await _userProcessor.IsUserAuthTokenValidAsync(result.CreatedId, token!);

        Assert.False(isValid);
    }

    [Fact]
    public async Task IsUserAuthTokenValidAsync_ReturnsFalse_WithInvalidToken()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        var result = await _userProcessor.CreateUserAsync(request);

        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken());

        var isValid = await _userProcessor.IsUserAuthTokenValidAsync(
            result.CreatedId,
            token! + "invalid"
        );

        Assert.False(isValid);
    }

    [Fact]
    public async Task GetAsymmetricSignatureVerificationKeyAsync_ReturnsKey()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        var result = await _userProcessor.CreateUserAsync(request);

        var key = await _userProcessor.GetAsymmetricSignatureVerificationKeyAsync(result.CreatedId);

        Assert.NotNull(key);
    }

    [Fact]
    public async Task GetAsymmetricSignatureVerificationKeyAsync_ReturnsNull_WhenUserDNE()
    {
        var key = await _userProcessor.GetAsymmetricSignatureVerificationKeyAsync(
            _fixture.Create<int>()
        );

        Assert.Null(key);
    }

    [Fact]
    public async Task GetAsymmetricSignatureVerificationKeyAsync_ReturnsNull_WhenSignatureKeyDNE()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        var result = await _userProcessor.CreateUserAsync(request);

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var user = await userRepo.GetAsync(u => u.Id == result.CreatedId);
        user?.AsymmetricKeys?.Clear();
        await userRepo.UpdateAsync(user!);

        var key = await _userProcessor.GetAsymmetricSignatureVerificationKeyAsync(result.CreatedId);

        Assert.Null(key);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_Fails_WhenUserDoesNotExist()
    {
        var request = new AssignRolesToUserRequest([], _fixture.Create<int>());
        var result = await _userProcessor.AssignRolesToUserAsync(request);
        Assert.Equal(AssignRolesToUserResultCode.UserNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_Fails_WhenRolesNotProvided()
    {
        var (userId, _) = await CreateUserAsync();
        var request = new AssignRolesToUserRequest([], userId);

        var result = await _userProcessor.AssignRolesToUserAsync(request);

        Assert.Equal(AssignRolesToUserResultCode.InvalidRoleIdsError, result.ResultCode);
        Assert.Equal(
            "All role ids are invalid (not found, already assigned to user, or from different account)",
            result.Message
        );
    }

    [Fact]
    public async Task AssignRolesToUserAsync_Succeeds_WhenRolesAssigned()
    {
        var (userId, accountId) = await CreateUserAsync();
        var roleId = await CreateRoleAsync(accountId);
        var request = new AssignRolesToUserRequest([roleId], userId);

        var result = await _userProcessor.AssignRolesToUserAsync(request);

        Assert.Equal(AssignRolesToUserResultCode.Success, result.ResultCode);

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var userEntity = await userRepo.GetAsync(
            g => g.Id == userId,
            include: u => u.Include(u => u.Roles)
        );

        Assert.NotNull(userEntity);
        Assert.NotNull(userEntity.Roles);
        Assert.Contains(userEntity.Roles, r => r.Id == roleId);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_PartiallySucceeds_WhenSomeRolesExistForUser()
    {
        var (userId, accountId) = await CreateUserAsync();
        var existingRoleId = await CreateRoleAsync(accountId, userId);
        var newRoleId = await CreateRoleAsync(accountId);
        var request = new AssignRolesToUserRequest([existingRoleId, newRoleId], userId);

        var result = await _userProcessor.AssignRolesToUserAsync(request);

        Assert.Equal(AssignRolesToUserResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some role ids are invalid (not found, already assigned to user, or from different account)",
            result.Message
        );
        Assert.Equal(1, result.AddedRoleCount);
        Assert.NotEmpty(result.InvalidRoleIds);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_PartiallySucceeds_WhenSomeRolesDoNotExist()
    {
        var (userId, accountId) = await CreateUserAsync();
        var roleId = await CreateRoleAsync(accountId);
        var request = new AssignRolesToUserRequest([roleId, -1], userId);

        var result = await _userProcessor.AssignRolesToUserAsync(request);

        Assert.Equal(AssignRolesToUserResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some role ids are invalid (not found, already assigned to user, or from different account)",
            result.Message
        );
        Assert.NotEmpty(result.InvalidRoleIds);
        Assert.Contains(-1, result.InvalidRoleIds);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_PartiallySucceeds_WhenSomeRolesBelongToDifferentAccount()
    {
        var (userId, accountId) = await CreateUserAsync();
        var roleIdSameAccount = await CreateRoleAsync(accountId);
        var roleIdDifferentAccount = await CreateRoleAsync(accountId + 1);
        var request = new AssignRolesToUserRequest(
            [roleIdSameAccount, roleIdDifferentAccount],
            userId
        );

        var result = await _userProcessor.AssignRolesToUserAsync(request);

        Assert.Equal(AssignRolesToUserResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some role ids are invalid (not found, already assigned to user, or from different account)",
            result.Message
        );
        Assert.Equal(1, result.AddedRoleCount);
        Assert.NotEmpty(result.InvalidRoleIds);
        Assert.Contains(roleIdDifferentAccount, result.InvalidRoleIds);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_Fails_WhenAllRolesExistForUser()
    {
        var (userId, accountId) = await CreateUserAsync();
        var roleIds = new List<int>()
        {
            await CreateRoleAsync(accountId, userId),
            await CreateRoleAsync(accountId, userId),
        };
        var request = new AssignRolesToUserRequest(roleIds, userId);
        await _userProcessor.AssignRolesToUserAsync(request);

        var result = await _userProcessor.AssignRolesToUserAsync(request);

        Assert.Equal(AssignRolesToUserResultCode.InvalidRoleIdsError, result.ResultCode);
        Assert.Equal(
            "All role ids are invalid (not found, already assigned to user, or from different account)",
            result.Message
        );
        Assert.Equal(0, result.AddedRoleCount);
        Assert.Equal(roleIds, result.InvalidRoleIds);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_Fails_WhenAllRolesDoNotExist()
    {
        var (userId, _) = await CreateUserAsync();
        var roleIds = _fixture.CreateMany<int>().ToList();
        var request = new AssignRolesToUserRequest(roleIds, userId);

        var result = await _userProcessor.AssignRolesToUserAsync(request);

        Assert.Equal(AssignRolesToUserResultCode.InvalidRoleIdsError, result.ResultCode);
        Assert.Equal(
            "All role ids are invalid (not found, already assigned to user, or from different account)",
            result.Message
        );
        Assert.Equal(0, result.AddedRoleCount);
        Assert.Equal(roleIds, result.InvalidRoleIds);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_Fails_WhenAllRolesBelongToDifferentAccount()
    {
        var (userId, accountId) = await CreateUserAsync();
        var roleIds = new List<int>()
        {
            await CreateRoleAsync(accountId + 1),
            await CreateRoleAsync(accountId + 2),
        };
        var request = new AssignRolesToUserRequest(roleIds, userId);

        var result = await _userProcessor.AssignRolesToUserAsync(request);

        Assert.Equal(AssignRolesToUserResultCode.InvalidRoleIdsError, result.ResultCode);
        Assert.Equal(
            "All role ids are invalid (not found, already assigned to user, or from different account)",
            result.Message
        );
        Assert.Equal(0, result.AddedRoleCount);
        Assert.Equal(roleIds, result.InvalidRoleIds);
    }
}
