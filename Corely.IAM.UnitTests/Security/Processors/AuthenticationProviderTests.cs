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

    private readonly Fixture _fixture = new();
    private readonly ServiceFactory _serviceFactory = new();
    private readonly AuthenticationProvider _authenticationProvider;

    public AuthenticationProviderTests()
    {
        _authenticationProvider = new AuthenticationProvider(
            _serviceFactory.GetRequiredService<IRepo<UserEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<UserAuthTokenEntity>>(),
            _serviceFactory.GetRequiredService<ISecurityProvider>(),
            _serviceFactory.GetRequiredService<IOptions<SecurityOptions>>(),
            _serviceFactory.GetRequiredService<ILogger<AuthenticationProvider>>()
        );
    }

    private async Task<int> CreateUserAsync()
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
            Accounts = [],
            Groups = [],
            Roles = [],
        };
        var created = await userRepo.CreateAsync(user);
        return created.Id;
    }

    private async Task<(int UserId, Guid UserPublicId)> CreateUserWithPublicIdAsync()
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
            Accounts = [],
            Groups = [],
            Roles = [],
        };
        var created = await userRepo.CreateAsync(user);
        return (created.Id, created.PublicId);
    }

    private async Task<int> CreateUserWithAccountAsync(int accountId)
    {
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var securityProcessor = _serviceFactory.GetRequiredService<ISecurityProvider>();

        var account = await accountRepo.GetAsync(a => a.Id == accountId);
        var signatureKey = securityProcessor.GetAsymmetricSignatureKeyEncryptedWithSystemKey();

        var user = new UserEntity
        {
            Username = _fixture.Create<string>(),
            Email = _fixture.Create<string>() + "@test.com",
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
            Accounts = account != null ? [account] : [],
            Groups = [],
            Roles = [],
        };
        var created = await userRepo.CreateAsync(user);
        return created.Id;
    }

    private async Task<(int AccountId, Guid AccountPublicId)> CreateAccountAsync()
    {
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var account = new AccountEntity { AccountName = _fixture.Create<string>() };
        var created = await accountRepo.CreateAsync(account);
        return (created.Id, created.PublicId);
    }

    private async Task<int> CreateUserWithoutSignatureKeyAsync()
    {
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();

        var user = new UserEntity
        {
            Username = _fixture.Create<string>(),
            Email = _fixture.Create<string>() + "@test.com",
            SymmetricKeys = [],
            AsymmetricKeys = [],
            Accounts = [],
            Groups = [],
            Roles = [],
        };
        var created = await userRepo.CreateAsync(user);
        return created.Id;
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_ReturnsSuccess()
    {
        var (userId, userPublicId) = await CreateUserWithPublicIdAsync();

        var authTokenResult = await _authenticationProvider.GetUserAuthTokenAsync(
            new UserAuthTokenRequest(userId)
        );

        Assert.Equal(UserAuthTokenResultCode.Success, authTokenResult.ResultCode);
        Assert.NotNull(authTokenResult.Token);
        Assert.NotNull(authTokenResult.Accounts);

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(authTokenResult.Token);

        Assert.Equal(typeof(AuthenticationProvider).FullName, jwtToken.Issuer);
        Assert.Equal(UserConstants.JWT_AUDIENCE, jwtToken.Audiences.First());
        // JWT now contains public ID (GUID) instead of internal ID
        Assert.Contains(
            jwtToken.Claims,
            c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userPublicId.ToString()
        );
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Jti);
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Iat);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_ReturnsUserNotFound_WhenUserDNE()
    {
        var result = await _authenticationProvider.GetUserAuthTokenAsync(
            new UserAuthTokenRequest(9999)
        );

        Assert.Equal(UserAuthTokenResultCode.UserNotFound, result.ResultCode);
        Assert.Null(result.Token);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_ReturnsSignatureKeyNotFound_WhenNoSignatureKey()
    {
        var userId = await CreateUserWithoutSignatureKeyAsync();

        var result = await _authenticationProvider.GetUserAuthTokenAsync(
            new UserAuthTokenRequest(userId)
        );

        Assert.Equal(UserAuthTokenResultCode.SignatureKeyNotFound, result.ResultCode);
        Assert.Null(result.Token);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_ReturnsAccountNotFound_WhenAccountNotBelongsToUser()
    {
        var userId = await CreateUserAsync();
        var (_, accountPublicId) = await CreateAccountAsync();

        var result = await _authenticationProvider.GetUserAuthTokenAsync(
            new UserAuthTokenRequest(userId, accountPublicId)
        );

        Assert.Equal(UserAuthTokenResultCode.AccountNotFound, result.ResultCode);
        Assert.Null(result.Token);
        Assert.Empty(result.Accounts);
    }

    [Fact]
    public async Task GetUserAuthTokenAsync_ReturnsSuccess_WithValidAccount()
    {
        var (accountId, accountPublicId) = await CreateAccountAsync();
        var userId = await CreateUserWithAccountAsync(accountId);

        var result = await _authenticationProvider.GetUserAuthTokenAsync(
            new UserAuthTokenRequest(userId, accountPublicId)
        );

        Assert.Equal(UserAuthTokenResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Token);
        Assert.Equal(accountId, result.SignedInAccountId);
        Assert.Contains(result.Accounts, a => a.Id == accountId);
    }

    [Fact]
    public async Task ValidateUserAuthTokenAsync_ReturnsSuccess_WithValidToken()
    {
        var userId = await CreateUserAsync();
        var authTokenResult = await _authenticationProvider.GetUserAuthTokenAsync(
            new UserAuthTokenRequest(userId)
        );

        var validationResult = await _authenticationProvider.ValidateUserAuthTokenAsync(
            authTokenResult.Token!
        );

        Assert.Equal(UserAuthTokenValidationResultCode.Success, validationResult.ResultCode);
        Assert.Equal(userId, validationResult.UserId);
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
    }

    [Fact]
    public async Task ValidateUserAuthTokenAsync_ReturnsTokenValidationFailed_WhenSignatureInvalid()
    {
        var userId = await CreateUserAsync();
        var authTokenResult = await _authenticationProvider.GetUserAuthTokenAsync(
            new UserAuthTokenRequest(userId)
        );

        var validationResult = await _authenticationProvider.ValidateUserAuthTokenAsync(
            authTokenResult.Token! + "tampered"
        );

        Assert.Equal(
            UserAuthTokenValidationResultCode.TokenValidationFailed,
            validationResult.ResultCode
        );
        Assert.Null(validationResult.UserId);
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
    }

    [Fact]
    public async Task ValidateUserAuthTokenAsync_ReturnsMissingUserIdClaim_WhenSubClaimNotGuid()
    {
        // Sub claim is an integer, not a GUID - should fail
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
    }

    [Fact]
    public async Task ValidateUserAuthTokenAsync_ReturnsTokenValidationFailed_WhenTokenNotTracked()
    {
        var (_, userPublicId) = await CreateUserWithPublicIdAsync();

        var token = new JwtSecurityToken(
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userPublicId.ToString()),
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
    }

    [Fact]
    public async Task ValidateUserAuthTokenAsync_ReturnsTokenValidationFailed_WhenTokenRevoked()
    {
        var userId = await CreateUserAsync();
        var authTokenResult = await _authenticationProvider.GetUserAuthTokenAsync(
            new UserAuthTokenRequest(userId)
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(authTokenResult.Token!);
        var tokenId = jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        await _authenticationProvider.RevokeUserAuthTokenAsync(userId, tokenId);

        var validationResult = await _authenticationProvider.ValidateUserAuthTokenAsync(
            authTokenResult.Token!
        );

        Assert.Equal(
            UserAuthTokenValidationResultCode.TokenValidationFailed,
            validationResult.ResultCode
        );
        Assert.Null(validationResult.UserId);
    }

    [Fact]
    public async Task RevokeUserAuthTokenAsync_ReturnsTrue_WhenTokenRevoked()
    {
        var userId = await CreateUserAsync();
        var authTokenResult = await _authenticationProvider.GetUserAuthTokenAsync(
            new UserAuthTokenRequest(userId)
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(authTokenResult.Token!);
        var tokenId = jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        var result = await _authenticationProvider.RevokeUserAuthTokenAsync(userId, tokenId);

        Assert.True(result);
    }

    [Fact]
    public async Task RevokeUserAuthTokenAsync_ReturnsFalse_WhenTokenNotFound()
    {
        var result = await _authenticationProvider.RevokeUserAuthTokenAsync(
            9999,
            "non-existent-token-id"
        );

        Assert.False(result);
    }

    [Fact]
    public async Task RevokeAllUserAuthTokensAsync_RevokesAllTokens()
    {
        var userId = await CreateUserAsync();

        var token1 = await _authenticationProvider.GetUserAuthTokenAsync(
            new UserAuthTokenRequest(userId)
        );
        var token2 = await _authenticationProvider.GetUserAuthTokenAsync(
            new UserAuthTokenRequest(userId)
        );

        await _authenticationProvider.RevokeAllUserAuthTokensAsync(userId);

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
