using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Constants;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corely.IAM.UnitTests.Security.Processors;

public class AuthenticationProviderTests
{
    private const string VALID_USERNAME = "username";
    private const string VALID_EMAIL = "email@x.y";
    private const string TEST_DEVICE_ID = "test-device";

    private readonly Fixture _fixture = new();
    private readonly ServiceFactory _serviceFactory = new();
    private readonly AuthenticationProvider _authenticationProvider;

    public AuthenticationProviderTests()
    {
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        _fixture.Customize<AccountEntity>(c =>
            c.Without(a => a.SymmetricKeys)
                .Without(a => a.AsymmetricKeys)
                .Without(a => a.Users)
                .Without(a => a.Groups)
                .Without(a => a.Roles)
                .Without(a => a.Permissions)
        );

        _authenticationProvider = new AuthenticationProvider(
            _serviceFactory.GetRequiredService<IRepo<UserEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<UserAuthTokenEntity>>(),
            _serviceFactory.GetRequiredService<ISecurityProvider>(),
            _serviceFactory.GetRequiredService<IOptions<SecurityOptions>>(),
            _serviceFactory.GetRequiredService<ILogger<AuthenticationProvider>>(),
            TimeProvider.System
        );
    }

    private async Task<UserEntity> CreateUserAsync(List<AccountEntity>? accounts = null)
    {
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var securityProcessor = _serviceFactory.GetRequiredService<ISecurityProvider>();

        var signatureKey = securityProcessor.GetAsymmetricSignatureKeyEncryptedWithSystemKey();

        var user = new UserEntity
        {
            Username = VALID_USERNAME,
            Email = VALID_EMAIL,
            SymmetricKeys = [],
            AsymmetricKeys =
            [
                new UserAsymmetricKeyEntity
                {
                    KeyUsedFor = KeyUsedFor.Signature,
                    ProviderTypeCode = signatureKey.ProviderTypeCode,
                    PublicKey = signatureKey.PublicKey,
                    EncryptedPrivateKey = signatureKey.PrivateKey.Secret,
                },
            ],
            Accounts = accounts ?? [.. _fixture.CreateMany<AccountEntity>()],
            Groups = [],
            Roles = [],
        };
        var created = await userRepo.CreateAsync(user);
        return created;
    }

    private async Task<AccountEntity> CreateAccountAsync()
    {
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var account = new AccountEntity
        {
            AccountName = _fixture.Create<string>(),
            Id = _fixture.Create<int>(),
            PublicId = Guid.NewGuid(),
        };
        var created = await accountRepo.CreateAsync(account);
        return created;
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_ReturnsSuccess()
    {
        var userEntity = await CreateUserAsync();

        var authTokenResult = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, TEST_DEVICE_ID)
        );

        Assert.Equal(UserAuthTokenResultCode.Success, authTokenResult.ResultCode);
        Assert.NotNull(authTokenResult.Token);
        Assert.NotNull(authTokenResult.Accounts);

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(authTokenResult.Token);

        Assert.Equal(typeof(AuthenticationProvider).FullName, jwtToken.Issuer);
        Assert.Equal(UserConstants.JWT_AUDIENCE, jwtToken.Audiences.First());
        Assert.Contains(
            jwtToken.Claims,
            c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userEntity.PublicId.ToString()
        );
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Jti);
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Iat);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_ReturnsUserNotFound_WhenUserDNE()
    {
        var result = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(9999, TEST_DEVICE_ID)
        );

        Assert.Equal(UserAuthTokenResultCode.UserNotFound, result.ResultCode);
        Assert.Null(result.Token);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_ReturnsSignatureKeyNotFound_WhenNoSignatureKey()
    {
        var userEntity = await CreateUserAsync();
        userEntity.SymmetricKeys!.Clear();
        userEntity.AsymmetricKeys!.Clear();

        var result = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, TEST_DEVICE_ID)
        );

        Assert.Equal(UserAuthTokenResultCode.SignatureKeyNotFound, result.ResultCode);
        Assert.Null(result.Token);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_ReturnsAccountNotFound_WhenAccountNotBelongsToUser()
    {
        var userEntity = await CreateUserAsync();
        var account = await CreateAccountAsync();

        var result = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, TEST_DEVICE_ID, account.PublicId)
        );

        Assert.Equal(UserAuthTokenResultCode.AccountNotFound, result.ResultCode);
        Assert.Null(result.Token);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_ReturnsSuccess_WithValidAccount()
    {
        var account = await CreateAccountAsync();
        var userEntity = await CreateUserAsync([account]);

        var result = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, TEST_DEVICE_ID, account.PublicId)
        );

        Assert.Equal(UserAuthTokenResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.TokenId);
        Assert.Equal(account.Id, result.SignedInAccountId);
        Assert.Contains(result.Accounts, a => a.Id == account.Id);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_SetsAccountIdOnTokenEntity()
    {
        var account = await CreateAccountAsync();
        var userEntity = await CreateUserAsync([account]);

        var result = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, TEST_DEVICE_ID, account.PublicId)
        );

        var authTokenRepo = _serviceFactory.GetRequiredService<IRepo<UserAuthTokenEntity>>();
        var tokenEntity = await authTokenRepo.GetAsync(t => t.PublicId == result.TokenId);

        Assert.NotNull(tokenEntity);
        Assert.Equal(account.Id, tokenEntity.AccountId);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_SetsNullAccountIdOnTokenEntity_WhenNoAccountSpecified()
    {
        var userEntity = await CreateUserAsync();

        var result = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, TEST_DEVICE_ID)
        );

        var authTokenRepo = _serviceFactory.GetRequiredService<IRepo<UserAuthTokenEntity>>();
        var tokenEntity = await authTokenRepo.GetAsync(t => t.PublicId == result.TokenId);

        Assert.NotNull(tokenEntity);
        Assert.Null(tokenEntity.AccountId);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_RevokesExistingTokenForSameUserAndAccount()
    {
        var account = await CreateAccountAsync();
        var userEntity = await CreateUserAsync([account]);

        var firstToken = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, TEST_DEVICE_ID, account.PublicId)
        );

        var secondToken = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, TEST_DEVICE_ID, account.PublicId)
        );

        var firstValidation = await _authenticationProvider.ValidateUserAuthTokenAsync(
            firstToken.Token!
        );
        var secondValidation = await _authenticationProvider.ValidateUserAuthTokenAsync(
            secondToken.Token!
        );

        Assert.Equal(
            UserAuthTokenValidationResultCode.TokenValidationFailed,
            firstValidation.ResultCode
        );
        Assert.Equal(UserAuthTokenValidationResultCode.Success, secondValidation.ResultCode);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_RevokesExistingTokenForSameUserWithNullAccount()
    {
        var userEntity = await CreateUserAsync();

        var firstToken = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, TEST_DEVICE_ID)
        );

        var secondToken = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, TEST_DEVICE_ID)
        );

        var firstValidation = await _authenticationProvider.ValidateUserAuthTokenAsync(
            firstToken.Token!
        );
        var secondValidation = await _authenticationProvider.ValidateUserAuthTokenAsync(
            secondToken.Token!
        );

        Assert.Equal(
            UserAuthTokenValidationResultCode.TokenValidationFailed,
            firstValidation.ResultCode
        );
        Assert.Equal(UserAuthTokenValidationResultCode.Success, secondValidation.ResultCode);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_DoesNotRevokeTokenForDifferentAccount()
    {
        var account1 = await CreateAccountAsync();
        var account2 = await CreateAccountAsync();
        var userEntity = await CreateUserAsync([account1, account2]);

        var tokenForAccount1 = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, TEST_DEVICE_ID, account1.PublicId)
        );

        Assert.Equal(UserAuthTokenResultCode.Success, tokenForAccount1.ResultCode);
        Assert.Equal(account1.Id, tokenForAccount1.SignedInAccountId);

        var tokenForAccount2 = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, TEST_DEVICE_ID, account2.PublicId)
        );

        Assert.Equal(UserAuthTokenResultCode.Success, tokenForAccount2.ResultCode);
        Assert.Equal(account2.Id, tokenForAccount2.SignedInAccountId);

        Assert.NotEqual(account1.Id, account2.Id);

        var authTokenRepo = _serviceFactory.GetRequiredService<IRepo<UserAuthTokenEntity>>();
        var token1Entity = await authTokenRepo.GetAsync(t =>
            t.PublicId == tokenForAccount1.TokenId
        );
        var token2Entity = await authTokenRepo.GetAsync(t =>
            t.PublicId == tokenForAccount2.TokenId
        );

        Assert.NotNull(token1Entity);
        Assert.NotNull(token2Entity);
        Assert.Equal(account1.Id, token1Entity.AccountId);
        Assert.Equal(account2.Id, token2Entity.AccountId);
        Assert.Null(token1Entity.RevokedUtc);
        Assert.Null(token2Entity.RevokedUtc);

        var validation1 = await _authenticationProvider.ValidateUserAuthTokenAsync(
            tokenForAccount1.Token!
        );
        var validation2 = await _authenticationProvider.ValidateUserAuthTokenAsync(
            tokenForAccount2.Token!
        );

        Assert.Equal(UserAuthTokenValidationResultCode.Success, validation1.ResultCode);
        Assert.Equal(UserAuthTokenValidationResultCode.Success, validation2.ResultCode);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_DoesNotRevokeNullAccountToken_WhenSwitchingToAccount()
    {
        var account = await CreateAccountAsync();
        var userEntity = await CreateUserAsync([account]);

        var tokenWithoutAccount = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, TEST_DEVICE_ID)
        );

        var tokenWithAccount = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, TEST_DEVICE_ID, account.PublicId)
        );

        var validationWithoutAccount = await _authenticationProvider.ValidateUserAuthTokenAsync(
            tokenWithoutAccount.Token!
        );
        var validationWithAccount = await _authenticationProvider.ValidateUserAuthTokenAsync(
            tokenWithAccount.Token!
        );

        Assert.Equal(
            UserAuthTokenValidationResultCode.Success,
            validationWithoutAccount.ResultCode
        );
        Assert.Equal(UserAuthTokenValidationResultCode.Success, validationWithAccount.ResultCode);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_SetsDeviceIdOnTokenEntity()
    {
        var userEntity = await CreateUserAsync();
        var deviceId = "test-device-123";

        var result = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, deviceId)
        );

        var authTokenRepo = _serviceFactory.GetRequiredService<IRepo<UserAuthTokenEntity>>();
        var tokenEntity = await authTokenRepo.GetAsync(t => t.PublicId == result.TokenId);

        Assert.NotNull(tokenEntity);
        Assert.Equal(deviceId, tokenEntity.DeviceId);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_RevokesExistingTokenForSameUserAccountAndDevice()
    {
        var account = await CreateAccountAsync();
        var userEntity = await CreateUserAsync([account]);
        var deviceId = "test-device-456";

        var firstToken = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, deviceId, account.PublicId)
        );

        var secondToken = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, deviceId, account.PublicId)
        );

        var firstValidation = await _authenticationProvider.ValidateUserAuthTokenAsync(
            firstToken.Token!
        );
        var secondValidation = await _authenticationProvider.ValidateUserAuthTokenAsync(
            secondToken.Token!
        );

        Assert.Equal(
            UserAuthTokenValidationResultCode.TokenValidationFailed,
            firstValidation.ResultCode
        );
        Assert.Equal(UserAuthTokenValidationResultCode.Success, secondValidation.ResultCode);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_DoesNotRevokeTokenForDifferentDevice()
    {
        var userEntity = await CreateUserAsync();
        var device1 = "device-1";
        var device2 = "device-2";

        var tokenForDevice1 = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, device1)
        );

        var tokenForDevice2 = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, device2)
        );

        var validation1 = await _authenticationProvider.ValidateUserAuthTokenAsync(
            tokenForDevice1.Token!
        );
        var validation2 = await _authenticationProvider.ValidateUserAuthTokenAsync(
            tokenForDevice2.Token!
        );

        Assert.Equal(UserAuthTokenValidationResultCode.Success, validation1.ResultCode);
        Assert.Equal(UserAuthTokenValidationResultCode.Success, validation2.ResultCode);
    }

    [Fact]
    public async Task ValidateUserAuthTokenAsync_ReturnsSuccess_WithValidToken()
    {
        var userEntity = await CreateUserAsync();
        var authTokenResult = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, TEST_DEVICE_ID)
        );

        var validationResult = await _authenticationProvider.ValidateUserAuthTokenAsync(
            authTokenResult.Token!
        );

        Assert.Equal(UserAuthTokenValidationResultCode.Success, validationResult.ResultCode);
        Assert.Equal(userEntity.Id, validationResult.UserId);
        Assert.Null(validationResult.SignedInAccountId);
        Assert.Equal(
            userEntity.Accounts!.Select(a => a.Id).OrderBy(id => id),
            validationResult.Accounts.Select(a => a.Id).OrderBy(id => id)
        );
        Assert.Equal(
            authTokenResult.Accounts!.Select(a => a.Id).OrderBy(id => id),
            validationResult.Accounts.Select(a => a.Id).OrderBy(id => id)
        );
    }

    [Fact]
    public async Task ValidateUserAuthTokenAsync_ReturnsInvalidTokenFormat_WithInvalidTokenFormat()
    {
        var validationResult = await _authenticationProvider.ValidateUserAuthTokenAsync(
            "not-a-jwt"
        );

        Assert.Equal(
            UserAuthTokenValidationResultCode.InvalidTokenFormat,
            validationResult.ResultCode
        );
        Assert.Null(validationResult.UserId);
        Assert.Null(validationResult.SignedInAccountId);
        Assert.Empty(validationResult.Accounts);
    }

    [Fact]
    public async Task ValidateUserAuthTokenAsync_ReturnsTokenValidationFailed_WhenSignatureInvalid()
    {
        var userEntity = await CreateUserAsync();
        var authTokenResult = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, TEST_DEVICE_ID)
        );

        var validationResult = await _authenticationProvider.ValidateUserAuthTokenAsync(
            authTokenResult.Token! + "tampered"
        );

        Assert.Equal(
            UserAuthTokenValidationResultCode.TokenValidationFailed,
            validationResult.ResultCode
        );
        Assert.Null(validationResult.UserId);
        Assert.Null(validationResult.SignedInAccountId);
        Assert.Empty(validationResult.Accounts);
    }

    [Fact]
    public async Task ValidateUserAuthTokenAsync_ReturnsMissingUserIdClaim_WhenNoSubClaim()
    {
        var token = new JwtSecurityToken(
            claims: [new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())],
            expires: DateTime.UtcNow.AddHours(1)
        );
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var validationResult = await _authenticationProvider.ValidateUserAuthTokenAsync(
            tokenString
        );

        Assert.Equal(
            UserAuthTokenValidationResultCode.MissingUserIdClaim,
            validationResult.ResultCode
        );
        Assert.Null(validationResult.UserId);
        Assert.Null(validationResult.SignedInAccountId);
        Assert.Empty(validationResult.Accounts);
    }

    [Fact]
    public async Task ValidateUserAuthTokenAsync_ReturnsMissingUserIdClaim_WhenSubClaimNotGuid()
    {
        var token = new JwtSecurityToken(
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, "123"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ],
            expires: DateTime.UtcNow.AddHours(1)
        );
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var validationResult = await _authenticationProvider.ValidateUserAuthTokenAsync(
            tokenString
        );

        Assert.Equal(
            UserAuthTokenValidationResultCode.MissingUserIdClaim,
            validationResult.ResultCode
        );
        Assert.Null(validationResult.UserId);
        Assert.Null(validationResult.SignedInAccountId);
        Assert.Empty(validationResult.Accounts);
    }

    [Fact]
    public async Task ValidateUserAuthTokenAsync_ReturnsTokenValidationFailed_WhenNoJtiClaim()
    {
        var token = new JwtSecurityToken(
            claims: [new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())],
            expires: DateTime.UtcNow.AddHours(1)
        );
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var validationResult = await _authenticationProvider.ValidateUserAuthTokenAsync(
            tokenString
        );

        Assert.Equal(
            UserAuthTokenValidationResultCode.TokenValidationFailed,
            validationResult.ResultCode
        );
        Assert.Null(validationResult.UserId);
        Assert.Null(validationResult.SignedInAccountId);
        Assert.Empty(validationResult.Accounts);
    }

    [Fact]
    public async Task ValidateUserAuthTokenAsync_ReturnsTokenValidationFailed_WhenTokenNotTracked()
    {
        var userEntity = await CreateUserAsync();

        var token = new JwtSecurityToken(
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userEntity.PublicId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ],
            expires: DateTime.UtcNow.AddHours(1)
        );
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var validationResult = await _authenticationProvider.ValidateUserAuthTokenAsync(
            tokenString
        );

        Assert.Equal(
            UserAuthTokenValidationResultCode.TokenValidationFailed,
            validationResult.ResultCode
        );
        Assert.Null(validationResult.UserId);
        Assert.Null(validationResult.SignedInAccountId);
        Assert.Empty(validationResult.Accounts);
    }

    [Fact]
    public async Task ValidateUserAuthTokenAsync_ReturnsTokenValidationFailed_WhenTokenRevoked()
    {
        var userEntity = await CreateUserAsync();
        var authTokenResult = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, TEST_DEVICE_ID)
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(authTokenResult.Token!);
        var tokenId = jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        var revokeRequest = new RevokeUserAuthTokenRequest(userEntity.Id, tokenId, TEST_DEVICE_ID);
        await _authenticationProvider.RevokeUserAuthTokenAsync(revokeRequest);

        var validationResult = await _authenticationProvider.ValidateUserAuthTokenAsync(
            authTokenResult.Token!
        );

        Assert.Equal(
            UserAuthTokenValidationResultCode.TokenValidationFailed,
            validationResult.ResultCode
        );
        Assert.Null(validationResult.UserId);
        Assert.Null(validationResult.SignedInAccountId);
        Assert.Empty(validationResult.Accounts);
    }

    [Fact]
    public async Task RevokeUserAuthTokenAsync_ReturnsTrue_WhenTokenRevoked()
    {
        var userEntity = await CreateUserAsync();
        var authTokenResult = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, TEST_DEVICE_ID)
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(authTokenResult.Token!);
        var tokenId = jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        var revokeRequest = new RevokeUserAuthTokenRequest(userEntity.Id, tokenId, TEST_DEVICE_ID);
        var result = await _authenticationProvider.RevokeUserAuthTokenAsync(revokeRequest);

        Assert.True(result);
    }

    [Fact]
    public async Task RevokeUserAuthTokenAsync_ReturnsFalse_WhenTokenNotFound()
    {
        var revokeRequest = new RevokeUserAuthTokenRequest(
            9999,
            "non-existent-token-id",
            TEST_DEVICE_ID
        );
        var result = await _authenticationProvider.RevokeUserAuthTokenAsync(revokeRequest);

        Assert.False(result);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_IncludesDeviceIdClaim_InJwtToken()
    {
        var userEntity = await CreateUserAsync();
        var deviceId = "test-device-claim-check";

        var result = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, deviceId)
        );

        Assert.Equal(UserAuthTokenResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Token);

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(result.Token);

        var deviceIdClaim = jwtToken.Claims.FirstOrDefault(c =>
            c.Type == UserConstants.DEVICE_ID_CLAIM
        );
        Assert.NotNull(deviceIdClaim);
        Assert.Equal(deviceId, deviceIdClaim.Value);
    }

    [Fact]
    public async Task ValidateUserAuthTokenAsync_ReturnsDeviceId_FromJwtToken()
    {
        var userEntity = await CreateUserAsync();
        var deviceId = "test-device-validation-check";

        var authTokenResult = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, deviceId)
        );

        var validationResult = await _authenticationProvider.ValidateUserAuthTokenAsync(
            authTokenResult.Token!
        );

        Assert.Equal(UserAuthTokenValidationResultCode.Success, validationResult.ResultCode);
        Assert.Equal(deviceId, validationResult.DeviceId);
    }

    [Fact]
    public async Task RevokeAllUserAuthTokensAsync_RevokesAllTokens()
    {
        var userEntity = await CreateUserAsync();

        var token1 = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, "device-1")
        );
        var token2 = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userEntity.Id, "device-2")
        );

        await _authenticationProvider.RevokeAllUserAuthTokensAsync(userEntity.Id);

        var validation1 = await _authenticationProvider.ValidateUserAuthTokenAsync(token1.Token!);
        var validation2 = await _authenticationProvider.ValidateUserAuthTokenAsync(token2.Token!);

        Assert.Equal(
            UserAuthTokenValidationResultCode.TokenValidationFailed,
            validation1.ResultCode
        );
        Assert.Equal(
            UserAuthTokenValidationResultCode.TokenValidationFailed,
            validation2.ResultCode
        );
    }
}
