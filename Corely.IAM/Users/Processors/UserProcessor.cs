using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Processors;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Mappers;
using Corely.IAM.Users.Models;
using Corely.IAM.Validators;
using Corely.Security.Encryption.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Corely.IAM.Users.Processors;

internal class UserProcessor : IUserProcessor
{
    private readonly IRepo<UserEntity> _userRepo;
    private readonly IRepo<UserAuthTokenEntity> _authTokenRepo;
    private readonly IReadonlyRepo<RoleEntity> _roleRepo;
    private readonly ISecurityProcessor _securityProcessor;
    private readonly ISymmetricEncryptionProviderFactory _encryptionProviderFactory;
    private readonly IValidationProvider _validationProvider;
    private readonly SecurityOptions _securityOptions;
    private readonly ILogger<UserProcessor> _logger;

    public UserProcessor(
        IRepo<UserEntity> userRepo,
        IRepo<UserAuthTokenEntity> authTokenRepo,
        IReadonlyRepo<RoleEntity> roleRepo,
        ISecurityProcessor securityProcessor,
        ISymmetricEncryptionProviderFactory encryptionProviderFactory,
        IValidationProvider validationProvider,
        IOptions<SecurityOptions> securityOptions,
        ILogger<UserProcessor> logger
    )
    {
        _userRepo = userRepo.ThrowIfNull(nameof(userRepo));
        _authTokenRepo = authTokenRepo.ThrowIfNull(nameof(authTokenRepo));
        _roleRepo = roleRepo.ThrowIfNull(nameof(roleRepo));
        _securityProcessor = securityProcessor.ThrowIfNull(nameof(securityProcessor));
        _encryptionProviderFactory = encryptionProviderFactory.ThrowIfNull(
            nameof(encryptionProviderFactory)
        );
        _validationProvider = validationProvider.ThrowIfNull(nameof(validationProvider));
        _securityOptions = securityOptions.ThrowIfNull(nameof(securityOptions)).Value;
        _logger = logger.ThrowIfNull(nameof(logger));
    }

    public async Task<CreateUserResult> CreateUserAsync(CreateUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var user = request.ToUser();
        _validationProvider.ThrowIfInvalid(user);

        var existingUser = await _userRepo.GetAsync(u =>
            u.Username == request.Username || u.Email == request.Email
        );

        if (existingUser != null)
        {
            bool usernameExists = existingUser.Username == request.Username;
            bool emailExists = existingUser.Email == request.Email;

            if (usernameExists)
                _logger.LogWarning(
                    "User already exists with Username {ExistingUsername}",
                    existingUser.Username
                );
            if (emailExists)
                _logger.LogWarning(
                    "User already exists with Email {ExistingEmail}",
                    existingUser.Email
                );

            string usernameExistsMessage = usernameExists
                ? $"Username {request.Username} already exists."
                : string.Empty;
            string emailExistsMessage = emailExists
                ? $"Email {request.Email} already exists."
                : string.Empty;

            return new CreateUserResult(
                CreateUserResultCode.UserExistsError,
                $"{usernameExistsMessage} {emailExistsMessage}".Trim(),
                -1
            );
        }

        user.SymmetricKeys = [_securityProcessor.GetSymmetricEncryptionKeyEncryptedWithSystemKey()];
        user.AsymmetricKeys =
        [
            _securityProcessor.GetAsymmetricEncryptionKeyEncryptedWithSystemKey(),
            _securityProcessor.GetAsymmetricSignatureKeyEncryptedWithSystemKey(),
        ];

        var userEntity = user.ToEntity(_encryptionProviderFactory); // user is validated
        var created = await _userRepo.CreateAsync(userEntity);

        return new CreateUserResult(CreateUserResultCode.Success, string.Empty, created.Id);
    }

    public async Task<User?> GetUserAsync(int userId)
    {
        var userEntity = await _userRepo.GetAsync(u => u.Id == userId);

        if (userEntity == null)
        {
            _logger.LogInformation("User with Id {UserId} not found", userId);
            return null;
        }

        return userEntity.ToModel();
    }

    public async Task<User?> GetUserAsync(string userName)
    {
        var userEntity = await _userRepo.GetAsync(u => u.Username == userName);

        if (userEntity == null)
        {
            _logger.LogInformation("User with Username {Username} not found", userName);
            return null;
        }

        return userEntity.ToModel();
    }

    public async Task UpdateUserAsync(User user)
    {
        _validationProvider.ThrowIfInvalid(user);
        var userEntity = user.ToEntity();
        await _userRepo.UpdateAsync(userEntity);
    }

    public async Task<string?> GetUserAuthTokenAsync(int userId)
    {
        var userEntity = await _userRepo.GetAsync(
            u => u.Id == userId,
            include: q => q.Include(u => u.AsymmetricKeys).Include(u => u.Accounts)
        );

        if (userEntity == null)
        {
            _logger.LogWarning("User with Id {UserId} not found", userId);
            return null;
        }

        var signatureKey = userEntity.AsymmetricKeys?.FirstOrDefault(k =>
            k.KeyUsedFor == KeyUsedFor.Signature
        );
        if (signatureKey == null)
        {
            _logger.LogWarning(
                "User with Id {UserId} does not have an asymmetric signature key",
                userId
            );
            return null;
        }

        var privateKey = _securityProcessor.DecryptWithSystemKey(signatureKey.EncryptedPrivateKey);
        var credentials = _securityProcessor.GetAsymmetricSigningCredentials(
            signatureKey.ProviderTypeCode,
            privateKey,
            true
        );

        var now = DateTime.UtcNow;
        var expires = now.AddSeconds(_securityOptions.AuthTokenTtlSeconds);
        var jti = Guid.NewGuid().ToString();
        var accountIds = userEntity.Accounts?.Select(a => a.Id.ToString()) ?? [];

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(
                JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64
            ),
        };

        foreach (var accountId in accountIds)
        {
            claims.Add(new Claim("account_id", accountId));
        }

        var token = new JwtSecurityToken(
            issuer: typeof(UserProcessor).FullName,
            audience: "Corely.IAM",
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        // Track the token server-side
        var authTokenEntity = new UserAuthTokenEntity
        {
            UserId = userId,
            Jti = jti,
            IssuedUtc = now,
            ExpiresUtc = expires,
        };
        await _authTokenRepo.CreateAsync(authTokenEntity);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<bool> IsUserAuthTokenValidAsync(int userId, string authToken)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        if (!tokenHandler.CanReadToken(authToken))
        {
            _logger.LogInformation("Auth token is in invalid format");
            return false;
        }

        // Extract jti from token before signature validation
        var jwtToken = tokenHandler.ReadJwtToken(authToken);
        var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        if (string.IsNullOrEmpty(jti))
        {
            _logger.LogInformation("Auth token does not contain jti claim");
            return false;
        }

        // Check server-side token tracking
        var trackedToken = await _authTokenRepo.GetAsync(t =>
            t.Jti == jti
            && t.UserId == userId
            && t.RevokedUtc == null
            && t.ExpiresUtc > DateTime.UtcNow
        );

        if (trackedToken == null)
        {
            _logger.LogInformation("Auth token not found, revoked, or expired in server tracking");
            return false;
        }

        // Validate signature
        var signatureKey = await GetUserAsymmetricKeyAsync(userId, KeyUsedFor.Signature);
        if (signatureKey == null)
            return false;

        var credentials = _securityProcessor.GetAsymmetricSigningCredentials(
            signatureKey.ProviderTypeCode,
            signatureKey.PublicKey,
            false
        );

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = credentials.Key,
            ValidateIssuer = true,
            ValidIssuer = typeof(UserProcessor).FullName,
            ValidateAudience = true,
            ValidAudience = "Corely.IAM",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        try
        {
            tokenHandler.ValidateToken(authToken, validationParameters, out _);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Token validation failed: {Error}", ex.Message);
            return false;
        }
    }

    public async Task<bool> RevokeUserAuthTokenAsync(int userId, string jti)
    {
        var trackedToken = await _authTokenRepo.GetAsync(t =>
            t.Jti == jti
            && t.UserId == userId
            && t.RevokedUtc == null
            && t.ExpiresUtc > DateTime.UtcNow
        );

        if (trackedToken == null)
        {
            _logger.LogInformation(
                "Auth token with jti {Jti} not found, already revoked, or expired",
                jti
            );
            return false;
        }

        trackedToken.RevokedUtc = DateTime.UtcNow;
        await _authTokenRepo.UpdateAsync(trackedToken);

        _logger.LogInformation("Auth token with jti {Jti} revoked for user {UserId}", jti, userId);
        return true;
    }

    public async Task RevokeAllUserAuthTokensAsync(int userId)
    {
        var activeTokens = await _authTokenRepo.ListAsync(t =>
            t.UserId == userId && t.RevokedUtc == null && t.ExpiresUtc > DateTime.UtcNow
        );

        var now = DateTime.UtcNow;
        foreach (var token in activeTokens)
        {
            token.RevokedUtc = now;
            await _authTokenRepo.UpdateAsync(token);
        }

        if (activeTokens.Count > 0)
        {
            _logger.LogInformation(
                "Revoked {Count} auth tokens for user {UserId}",
                activeTokens.Count,
                userId
            );
        }
    }

    public async Task<string?> GetAsymmetricSignatureVerificationKeyAsync(int userId)
    {
        var signatureKey = await GetUserAsymmetricKeyAsync(userId, KeyUsedFor.Signature);
        if (signatureKey == null)
        {
            return null;
        }
        return signatureKey.PublicKey;
    }

    private async Task<UserAsymmetricKeyEntity?> GetUserAsymmetricKeyAsync(
        int userId,
        KeyUsedFor keyUse
    )
    {
        var userEntity = await _userRepo.GetAsync(
            u => u.Id == userId,
            include: q => q.Include(u => u.AsymmetricKeys)
        );

        if (userEntity == null)
        {
            _logger.LogWarning("User with Id {UserId} not found", userId);
            return null;
        }

        var signatureKey = userEntity.AsymmetricKeys?.FirstOrDefault(k => k.KeyUsedFor == keyUse);
        if (signatureKey == null)
        {
            _logger.LogWarning(
                "User with Id {UserId} does not have an asymmetric signature key",
                userId
            );
            return null;
        }

        return signatureKey;
    }

    public async Task<AssignRolesToUserResult> AssignRolesToUserAsync(
        AssignRolesToUserRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var userEntity = await _userRepo.GetAsync(
            u => u.Id == request.UserId,
            include: q => q.Include(u => u.Accounts)
        );
        if (userEntity == null)
        {
            _logger.LogWarning("User with Id {UserId} not found", request.UserId);
            return new AssignRolesToUserResult(
                AssignRolesToUserResultCode.UserNotFoundError,
                $"User with Id {request.UserId} not found",
                0,
                request.RoleIds
            );
        }
        var roleEntities = await _roleRepo.ListAsync(r =>
            request.RoleIds.Contains(r.Id) && !r.Users!.Any(u => u.Id == userEntity.Id)
        );

        roleEntities =
        [
            .. roleEntities.Where(r => userEntity.Accounts?.Any(a => a.Id == r.AccountId) ?? false),
        ];

        if (roleEntities.Count == 0)
        {
            _logger.LogInformation(
                "All role ids are invalid (not found, already assigned to user, or from different account) : {@InvalidRoleIds}",
                request.RoleIds
            );
            return new AssignRolesToUserResult(
                AssignRolesToUserResultCode.InvalidRoleIdsError,
                "All role ids are invalid (not found, already assigned to user, or from different account)",
                0,
                request.RoleIds
            );
        }

        userEntity.Roles ??= [];
        foreach (var role in roleEntities)
        {
            userEntity.Roles.Add(role);
        }

        await _userRepo.UpdateAsync(userEntity);

        var invalidRoleIds = request.RoleIds.Except(roleEntities.Select(r => r.Id)).ToList();
        if (invalidRoleIds.Count > 0)
        {
            _logger.LogInformation(
                "Some role ids are invalid (not found, already assigned to user, or from different account) : {@InvalidRoleIds}",
                invalidRoleIds
            );
            return new AssignRolesToUserResult(
                AssignRolesToUserResultCode.PartialSuccess,
                "Some role ids are invalid (not found, already assigned to user, or from different account)",
                roleEntities.Count,
                invalidRoleIds
            );
        }

        return new AssignRolesToUserResult(
            AssignRolesToUserResultCode.Success,
            string.Empty,
            roleEntities.Count,
            invalidRoleIds
        );
    }

    public async Task<DeleteUserResult> DeleteUserAsync(int userId)
    {
        var userEntity = await _userRepo.GetAsync(
            u => u.Id == userId,
            include: q => q.Include(u => u.Accounts)
        );

        if (userEntity == null)
        {
            _logger.LogWarning("User with Id {UserId} not found", userId);
            return new DeleteUserResult(
                DeleteUserResultCode.UserNotFoundError,
                $"User with Id {userId} not found"
            );
        }

        if (userEntity.Accounts != null)
        {
            foreach (var account in userEntity.Accounts)
            {
                var userHasOwnerRole = await _roleRepo.AnyAsync(r =>
                    r.AccountId == account.Id
                    && r.Name == RoleConstants.OWNER_ROLE_NAME
                    && (
                        r.Users!.Any(u => u.Id == userId)
                        || r.Groups!.Any(g => g.Users!.Any(u => u.Id == userId))
                    )
                );

                if (userHasOwnerRole)
                {
                    var otherOwnerExists = await _roleRepo.AnyAsync(r =>
                        r.AccountId == account.Id
                        && r.Name == RoleConstants.OWNER_ROLE_NAME
                        && (
                            r.Users!.Any(u =>
                                u.Id != userId && u.Accounts!.Any(a => a.Id == account.Id)
                            )
                            || r.Groups!.Any(g =>
                                g.Users!.Any(u =>
                                    u.Id != userId && u.Accounts!.Any(a => a.Id == account.Id)
                                )
                            )
                        )
                    );

                    if (!otherOwnerExists)
                    {
                        _logger.LogWarning(
                            "User with Id {UserId} is the sole owner of account {AccountId} and cannot be deleted",
                            userId,
                            account.Id
                        );
                        return new DeleteUserResult(
                            DeleteUserResultCode.UserIsSoleAccountOwnerError,
                            $"User is the sole owner of account '{account.AccountName}' (Id: {account.Id}) and cannot be deleted"
                        );
                    }
                }
            }
        }

        await _userRepo.DeleteAsync(userEntity);

        _logger.LogInformation("User with Id {UserId} deleted", userId);
        return new DeleteUserResult(DeleteUserResultCode.Success, string.Empty);
    }
}
