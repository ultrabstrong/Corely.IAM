using System.Text.RegularExpressions;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.DataAccess;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Platform.Mappers;
using Corely.IAM.Platform.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Providers;

public abstract class ProviderMatrixTestsBase(ProviderTestHost host) : IAsyncLifetime
{
    private const string Password = "Test1234";

    private Guid _ownerUserId;
    private Guid _accountId;
    private Guid _roleId;

    protected ProviderTestHost Host { get; } = host;

    public async ValueTask InitializeAsync()
    {
        if (DockerAvailability.UnavailableReason is not null)
            return;

        await Host.InitializeAsync();
        await Host.MigrateAsync();
        await SeedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (DockerAvailability.UnavailableReason is null)
            await Host.DisposeAsync();
    }

    [RequiresDockerFact]
    public async Task MigrationsApplyCleanly()
    {
        var applied = await Host.QueryAsync(db => db.Database.GetAppliedMigrationsAsync());

        Assert.NotEmpty(applied);
    }

    [RequiresDockerFact]
    public async Task NoPendingMigrationsRemain()
    {
        var pending = await Host.QueryAsync(db => db.Database.GetPendingMigrationsAsync());

        Assert.Empty(pending);
    }

    [RequiresDockerFact]
    public async Task RegistrationAndSignInWorkEndToEnd()
    {
        var result = await Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IAuthenticationService>()
                .SignInAsync(new SignInRequest("provider-owner", Password, "device"))
        );

        Assert.Equal(SignInResultCode.Success, result.ResultCode);
    }

    [RequiresDockerFact]
    public async Task DeletingARoleWithPermissions_DoesNotViolateForeignKeys()
    {
        var result = await ActAsOwnerAsync(services =>
            services
                .GetRequiredService<IDeregistrationService>()
                .DeregisterRoleAsync(new DeregisterRoleRequest(_roleId, _accountId))
        );

        Assert.Equal(DeregisterRoleResultCode.Success, result.ResultCode);
    }

    [RequiresDockerFact]
    public async Task SetBasedUpdatesTranslate()
    {
        await Host.WithScopeAsync(async services =>
        {
            var result = await services
                .GetRequiredService<IAuthenticationService>()
                .SignInAsync(new SignInRequest("provider-owner", Password, "second-device"));
            Assert.Equal(SignInResultCode.Success, result.ResultCode);
        });

        await ActAsOwnerAsync(async services =>
        {
            await services.GetRequiredService<IAuthenticationService>().SignOutAllAsync();
            return true;
        });

        var active = await Host.QueryAsync(db =>
            db.AuthTokens.AsNoTracking()
                .CountAsync(t => t.UserId == _ownerUserId && t.RevokedUtc == null)
        );

        Assert.Equal(0, active);
    }

    [RequiresDockerFact]
    public async Task AuthorizationResolvesAgainstRealProviderSql()
    {
        var authorized = await ActAsOwnerAsync(services =>
            services
                .GetRequiredService<IAuthorizationProvider>()
                .IsAuthorizedAsync(AuthAction.Read, Role.RESOURCE_TYPE, _roleId)
        );

        Assert.True(authorized, "The account owner must resolve as authorized on any provider.");
    }

    [RequiresDockerFact]
    public async Task PlatformSecuritySettingsRoundTrip()
    {
        var settings = SecurityPolicy.Default with { MaxLoginAttempts = 7, RequireDigit = false };
        await Host.QueryAsync(async db =>
        {
            db.PlatformSecuritySettings.Add(settings.ToEntity());
            return await db.SaveChangesAsync();
        });

        var stored = await Host.QueryAsync(db =>
            db.PlatformSecuritySettings.AsNoTracking().SingleAsync()
        );

        Assert.Equal(settings, stored.ToModel());
    }

    [RequiresDockerFact]
    public async Task Migrate_GivesTheDefaultApiKeyLifetime_ForAPlatformRowUpgradedWithZero()
    {
        await Host.QueryAsync(async db =>
        {
            await db.GetService<IMigrator>().MigrateAsync("ApiKeys");
            db.PlatformSecuritySettings.Add(
                (SecurityPolicy.Default with { ApiKeyMaxLifetimeDays = 0 }).ToEntity()
            );
            return await db.SaveChangesAsync();
        });

        await Host.MigrateAsync();

        Assert.Equal(
            SecurityPolicy.API_KEY_MAX_LIFETIME_DAYS_DEFAULT,
            await Host.QueryAsync(db =>
                db.PlatformSecuritySettings.AsNoTracking()
                    .Select(s => s.ApiKeyMaxLifetimeDays)
                    .SingleAsync()
            )
        );
    }

    [RequiresDockerFact]
    public async Task NullableComparisonsTranslate()
    {
        var exception = await Record.ExceptionAsync(() =>
            Host.QueryAsync(db =>
                db.AuthTokens.AsNoTracking()
                    .Where(t => t.AccountId == _accountId && t.RevokedUtc == null)
                    .ToListAsync()
            )
        );

        Assert.Null(exception);
    }

    [RequiresDockerFact]
    public async Task RenewalRotatesWithinTheSession()
    {
        var signIn = await SignInAsync("renew-device");
        Host.TimeProvider.AdvanceSeconds(60);

        var renewed = await RenewAsync(signIn.RefreshToken!);

        Assert.Equal(RenewAuthTokenResultCode.Success, renewed.ResultCode);
        var rows = await Host.QueryAsync(db =>
            db.AuthTokens.AsNoTracking().Where(t => t.DeviceId == "renew-device").ToListAsync()
        );
        Assert.Equal(2, rows.Count);
        Assert.Single(rows.Select(r => r.SessionId).Distinct());
        Assert.Single(rows.Select(r => r.SessionStartedUtc).Distinct());
        Assert.Equal(
            renewed.AuthTokenId,
            rows.Single(r => r.Id == signIn.AuthTokenId).ReplacedByTokenId
        );
    }

    [RequiresDockerFact]
    public async Task RenewalHonorsTheIdleExpiryToTheSecond()
    {
        var signIn = await SignInAsync("boundary-device");
        Host.TimeProvider.AdvanceSeconds(
            SecurityPolicy.AUTH_SESSION_IDLE_TIMEOUT_SECONDS_DEFAULT - 1
        );
        var justBefore = await RenewAsync(signIn.RefreshToken!);
        Host.TimeProvider.AdvanceSeconds(SecurityPolicy.AUTH_SESSION_IDLE_TIMEOUT_SECONDS_DEFAULT);

        var atExpiry = await RenewAsync(justBefore.RefreshToken!);

        Assert.Equal(RenewAuthTokenResultCode.Success, justBefore.ResultCode);
        Assert.Equal(RenewAuthTokenResultCode.SessionExpiredError, atExpiry.ResultCode);
    }

    [RequiresDockerFact]
    public async Task ParallelRenewalsOfOneToken_LetExactlyOneWin()
    {
        var signIn = await SignInAsync("race-device");
        Host.TimeProvider.AdvanceSeconds(60);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => Task.Run(() => RenewAsync(signIn.RefreshToken!)))
        );

        Assert.Single(results, r => r.ResultCode == RenewAuthTokenResultCode.Success);
        Assert.All(
            results.Where(r => r.ResultCode != RenewAuthTokenResultCode.Success),
            r =>
                Assert.Equal(RenewAuthTokenResultCode.RefreshTokenAlreadyRotatedError, r.ResultCode)
        );
        var active = await Host.QueryAsync(db =>
            db.AuthTokens.AsNoTracking()
                .CountAsync(t => t.DeviceId == "race-device" && t.RevokedUtc == null)
        );
        Assert.Equal(1, active);
    }

    [RequiresDockerFact]
    public async Task ExpiredTokenCleanupTranslates()
    {
        await SignInAsync("cleanup-device");
        Host.TimeProvider.AdvanceSeconds(
            SecurityPolicy.AUTH_SESSION_IDLE_TIMEOUT_SECONDS_DEFAULT + 1
        );

        var result = await Host.WithScopeAsync(services =>
        {
            var authentication = services.GetRequiredService<IAuthenticationService>();
            authentication.AuthenticateAsSystem("cleanup");
            return authentication.DeleteExpiredAuthTokensAsync();
        });

        Assert.Equal(DeleteExpiredAuthTokensResultCode.Success, result.ResultCode);
        var remaining = await Host.QueryAsync(db =>
            db.AuthTokens.AsNoTracking().CountAsync(t => t.DeviceId == "cleanup-device")
        );
        Assert.Equal(0, remaining);
    }

    [RequiresDockerFact]
    public async Task ApiKeysExchangeAndLoadPermissions_AndTheirRolesStayDeletable()
    {
        var created = await ActAsOwnerAsync(services =>
            services
                .GetRequiredService<IRegistrationService>()
                .RegisterApiKeyAsync(new RegisterApiKeyRequest("provider-key", _accountId))
        );
        var assigned = await ActAsOwnerAsync(services =>
            services
                .GetRequiredService<IRegistrationService>()
                .RegisterRolesWithApiKeyAsync(
                    new RegisterRolesWithApiKeyRequest(
                        [_roleId],
                        created.CreatedApiKeyId,
                        _accountId
                    )
                )
        );

        var canReadUsers = await Host.WithScopeAsync(async services =>
        {
            var signIn = await services
                .GetRequiredService<IAuthenticationService>()
                .SignInWithApiKeyAsync(new SignInWithApiKeyRequest(created.Key!, "key-device"));
            Assert.Equal(SignInResultCode.Success, signIn.ResultCode);
            return await services
                .GetRequiredService<IAuthorizationProvider>()
                .IsAuthorizedAsync(AuthAction.Read, User.RESOURCE_TYPE);
        });
        var roleDeleted = await ActAsOwnerAsync(services =>
            services
                .GetRequiredService<IDeregistrationService>()
                .DeregisterRoleAsync(new DeregisterRoleRequest(_roleId, _accountId))
        );

        Assert.Equal(CreateApiKeyResultCode.Success, created.ResultCode);
        Assert.Equal(AssignRolesToApiKeyResultCode.Success, assigned.ResultCode);
        Assert.True(canReadUsers);
        Assert.Equal(DeregisterRoleResultCode.Success, roleDeleted.ResultCode);
    }

    private Task<SignInResult> SignInAsync(string deviceId) =>
        Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IAuthenticationService>()
                .SignInAsync(new SignInRequest("provider-owner", Password, deviceId))
        );

    private Task<RenewAuthTokenResult> RenewAsync(string refreshToken) =>
        Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IAuthenticationService>()
                .RenewAuthTokenAsync(new RenewAuthTokenRequest(refreshToken))
        );

    private async Task SeedAsync()
    {
        _ownerUserId = await Host.WithScopeAsync(async services =>
        {
            var result = await services
                .GetRequiredService<IRegistrationService>()
                .RegisterUserAsync(
                    new RegisterUserRequest("provider-owner", "owner@example.com", Password)
                );
            Assert.Equal(RegisterUserResultCode.Success, result.ResultCode);
            return result.CreatedUserId;
        });

        _accountId = await Host.WithScopeAsync(async services =>
        {
            var signIn = await services
                .GetRequiredService<IAuthenticationService>()
                .SignInAsync(new SignInRequest("provider-owner", Password, "seed-device"));
            Assert.Equal(SignInResultCode.Success, signIn.ResultCode);

            var account = await services
                .GetRequiredService<IRegistrationService>()
                .RegisterAccountAsync(new RegisterAccountRequest("Provider", _ownerUserId));
            Assert.Equal(RegisterAccountResultCode.Success, account.ResultCode);
            return account.CreatedAccountId;
        });

        _roleId = await ActAsOwnerAsync(async services =>
        {
            var registration = services.GetRequiredService<IRegistrationService>();

            var role = await registration.RegisterRoleAsync(
                new RegisterRoleRequest("Provider Role", _accountId)
            );
            Assert.Equal(CreateRoleResultCode.Success, role.ResultCode);

            var permission = await registration.RegisterPermissionAsync(
                new RegisterPermissionRequest(
                    _accountId,
                    User.RESOURCE_TYPE,
                    Guid.Empty,
                    Read: true
                )
            );
            Assert.Equal(CreatePermissionResultCode.Success, permission.ResultCode);

            var assignment = await registration.RegisterPermissionsWithRoleAsync(
                new RegisterPermissionsWithRoleRequest(
                    [permission.CreatedPermissionId],
                    role.CreatedRoleId,
                    _accountId
                )
            );
            Assert.Equal(AssignPermissionsToRoleResultCode.Success, assignment.ResultCode);

            return role.CreatedRoleId;
        });
    }

    private Task<T> ActAsOwnerAsync<T>(Func<IServiceProvider, Task<T>> work) =>
        Host.WithScopeAsync(async services =>
        {
            var authentication = services.GetRequiredService<IAuthenticationService>();
            var signIn = await authentication.SignInAsync(
                new SignInRequest("provider-owner", Password, "act-device")
            );
            Assert.Equal(SignInResultCode.Success, signIn.ResultCode);

            var switched = await authentication.SwitchAccountAsync(
                new SwitchAccountRequest(_accountId)
            );
            Assert.Equal(SignInResultCode.Success, switched.ResultCode);

            return await work(services);
        });
}

public class MsSqlProviderMatrixTests()
    : ProviderMatrixTestsBase(new ProviderTestHost(DatabaseProvider.MsSql))
{
    private const string SCRIPT_DATABASE = "corely_iam_script";

    [RequiresDockerFact]
    public async Task IdempotentScript_AppliesAndReapplies_ForAnEmptyDatabase()
    {
        var script = await Host.GenerateIdempotentScriptAsync();
        await ExecuteAsync(
            Host.ConnectionStringFor("master"),
            $"CREATE DATABASE [{SCRIPT_DATABASE}]"
        );
        var connectionString = Host.ConnectionStringFor(SCRIPT_DATABASE);

        await ApplyScriptAsync(connectionString, script);
        await ApplyScriptAsync(connectionString, script);

        Assert.Equal(
            1,
            await ScalarAsync(
                connectionString,
                "SELECT CASE WHEN OBJECT_ID('AuthTokens') IS NOT NULL "
                    + "AND OBJECT_ID('ApiKeys') IS NOT NULL "
                    + "AND OBJECT_ID('UserAuthTokens') IS NULL THEN 1 ELSE 0 END"
            )
        );
    }

    private static async Task ApplyScriptAsync(string connectionString, string script)
    {
        foreach (
            var batch in Regex
                .Split(script, @"^\s*GO\s*$", RegexOptions.Multiline)
                .Where(b => !string.IsNullOrWhiteSpace(b))
        )
            await ExecuteAsync(connectionString, batch);
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<int> ScalarAsync(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        return (int)(await command.ExecuteScalarAsync())!;
    }
}

public class MySqlProviderMatrixTests()
    : ProviderMatrixTestsBase(new ProviderTestHost(DatabaseProvider.MySql)) { }
