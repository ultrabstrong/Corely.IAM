using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Audits.Entities;
using Corely.IAM.Audits.Enums;
using Corely.IAM.Audits.Models;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Groups.Models;
using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Platform;
using Corely.IAM.Platform.Models;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Entities;
using Corely.IAM.TotpAuths.Providers;
using Corely.IAM.Users.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Authorization;

public class PlatformAccountTests : IAsyncLifetime
{
    private readonly IamScenario _scenario = new();
    private Guid _platformAccountId;

    public async ValueTask InitializeAsync()
    {
        await _scenario.InitializeAsync();

        var result = await _scenario.Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IPlatformService>()
                .BootstrapPlatformAsync(
                    new BootstrapPlatformRequest(
                        "Platform",
                        "platform-owner",
                        "platform-owner@example.com"
                    )
                )
        );
        Assert.Equal(BootstrapPlatformResultCode.Success, result.ResultCode);
        _platformAccountId = result.Credentials!.AccountId;
    }

    public ValueTask DisposeAsync() => _scenario.DisposeAsync();

    [Fact]
    public async Task Bootstrap_FlagsAccountAndEnrollsOwnerTwoFactor_ForFirstRun()
    {
        var (isPlatform, totpEnabled) = await _scenario.Host.QueryAsync(async db =>
            (
                await db.Set<AccountEntity>()
                    .Where(a => a.Id == _platformAccountId)
                    .Select(a => a.IsPlatformAccount)
                    .SingleAsync(),
                await db.Set<TotpAuthEntity>()
                    .AnyAsync(t => t.User!.Username == "platform-owner" && t.IsEnabled)
            )
        );

        Assert.True(isPlatform);
        Assert.True(totpEnabled);
    }

    [Fact]
    public async Task Bootstrap_ChangesNothing_ForSecondRun()
    {
        var result = await _scenario.Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IPlatformService>()
                .BootstrapPlatformAsync(
                    new BootstrapPlatformRequest("Again", "second-owner", "second@example.com")
                )
        );

        Assert.Equal(BootstrapPlatformResultCode.AlreadyBootstrappedError, result.ResultCode);
        Assert.Equal(
            1,
            await _scenario.Host.QueryAsync(db =>
                db.Set<AccountEntity>().CountAsync(a => a.IsPlatformAccount)
            )
        );
    }

    [Fact]
    public async Task CompletePlatformOwnerPermissions_GivesFullAccess_ForEveryRegisteredType()
    {
        var fullAccessTypes = await _scenario.Host.QueryAsync(db =>
            db.Set<RoleEntity>()
                .Where(r => r.AccountId == _platformAccountId && r.Name == Role.OWNER_ROLE_NAME)
                .SelectMany(r => r.Permissions!)
                .Where(p =>
                    p.ResourceId == Guid.Empty
                    && p.Create
                    && p.Read
                    && p.Update
                    && p.Delete
                    && p.Execute
                    && p.IsSystemDefined
                )
                .Select(p => p.ResourceType)
                .ToListAsync()
        );

        Assert.Contains(IamScenario.INVOICE_RESOURCE_TYPE, fullAccessTypes);
        Assert.Contains(IamScenario.REPORT_RESOURCE_TYPE, fullAccessTypes);
        Assert.Contains(Account.RESOURCE_TYPE, fullAccessTypes);
    }

    [Fact]
    public async Task CompletePlatformOwnerPermissions_AddsNothing_ForCompleteOwner()
    {
        var added = await _scenario.Host.WithScopeAsync(services =>
            services.GetRequiredService<IPlatformService>().CompletePlatformOwnerPermissionsAsync()
        );

        Assert.Equal(0, added);
    }

    [Fact]
    public async Task Bootstrap_PutsViewerAndSetupRolesInTheSupportGroup_ForFirstRun()
    {
        var roles = await _scenario.Host.QueryAsync(db =>
            db.Set<GroupEntity>()
                .Where(g =>
                    g.AccountId == _platformAccountId && g.Name == Group.ACCOUNT_SUPPORT_GROUP_NAME
                )
                .SelectMany(g => g.Roles!)
                .Where(r => r.IsSystemDefined)
                .Select(r => r.Name)
                .ToListAsync()
        );

        Assert.Equal([Role.ACCOUNT_SETUP_ROLE_NAME, Role.ACCOUNT_VIEWER_ROLE_NAME], roles.Order());
    }

    [Fact]
    public async Task CompleteSupportDefaults_GivesViewerReadOnTypesThatAllowIt_ForEveryRegisteredType()
    {
        var viewer = await SupportRolePermissionsAsync(Role.ACCOUNT_VIEWER_ROLE_NAME);

        Assert.Contains($"{Account.RESOURCE_TYPE}:R", viewer);
        Assert.Contains($"{IamScenario.INVOICE_RESOURCE_TYPE}:R", viewer);
        Assert.Contains($"{AuditEntry.RESOURCE_TYPE}:R", viewer);
        Assert.All(viewer, p => Assert.EndsWith(":R", p));
        Assert.DoesNotContain(viewer, p => p.StartsWith($"{Account.SECRET_KEYS_RESOURCE_TYPE}:"));
        Assert.DoesNotContain(viewer, p => p.StartsWith($"{SecurityPolicy.RESOURCE_TYPE}:"));
        Assert.DoesNotContain(viewer, p => p.StartsWith($"{IamScenario.REPORT_RESOURCE_TYPE}:"));
    }

    [Fact]
    public async Task CompleteSupportDefaults_GivesSetupOnlyTheDeclaredChanges_ForSupportActions()
    {
        Assert.Equal(
            [$"{IamScenario.INVOICE_RESOURCE_TYPE}:C"],
            await SupportRolePermissionsAsync(Role.ACCOUNT_SETUP_ROLE_NAME)
        );
    }

    [Fact]
    public async Task CompleteSupportDefaults_ChangesNothing_ForCompleteSupport()
    {
        var changes = await _scenario.Host.WithScopeAsync(services =>
            services.GetRequiredService<PlatformSupportDefaults>().CompleteAsync()
        );

        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task CompleteSupportDefaults_AdoptsHandMadeRoleAndDropsExtras_ForRoleBuiltByHand()
    {
        await _scenario.Host.QueryAsync(async db =>
        {
            var setup = await db.Set<RoleEntity>()
                .Include(r => r.Permissions)
                .SingleAsync(r =>
                    r.AccountId == _platformAccountId && r.Name == Role.ACCOUNT_SETUP_ROLE_NAME
                );
            var ownerInvoice = await db.Set<PermissionEntity>()
                .SingleAsync(p =>
                    p.AccountId == _platformAccountId
                    && p.ResourceType == IamScenario.INVOICE_RESOURCE_TYPE
                    && p.Create
                    && p.Read
                    && p.Update
                    && p.Delete
                    && p.Execute
                );
            setup.IsSystemDefined = false;
            setup.Permissions!.Clear();
            setup.Permissions.Add(ownerInvoice);
            return await db.SaveChangesAsync();
        });

        var changes = await _scenario.Host.WithScopeAsync(services =>
            services.GetRequiredService<PlatformSupportDefaults>().CompleteAsync()
        );

        Assert.True(changes > 0);
        Assert.Equal(
            [$"{IamScenario.INVOICE_RESOURCE_TYPE}:C"],
            await SupportRolePermissionsAsync(Role.ACCOUNT_SETUP_ROLE_NAME)
        );
        Assert.Contains(
            $"{IamScenario.INVOICE_RESOURCE_TYPE}:CRUDX",
            await SupportRolePermissionsAsync(Role.OWNER_ROLE_NAME)
        );
    }

    [Fact]
    public async Task CompleteSupportDefaults_PutsRoleBackInGroup_ForRoleTakenOut()
    {
        await _scenario.Host.QueryAsync(async db =>
        {
            var group = await db.Set<GroupEntity>()
                .Include(g => g.Roles)
                .SingleAsync(g =>
                    g.AccountId == _platformAccountId && g.Name == Group.ACCOUNT_SUPPORT_GROUP_NAME
                );
            group.Roles!.Remove(group.Roles.Single(r => r.Name == Role.ACCOUNT_VIEWER_ROLE_NAME));
            return await db.SaveChangesAsync();
        });

        var changes = await _scenario.Host.WithScopeAsync(services =>
            services.GetRequiredService<PlatformSupportDefaults>().CompleteAsync()
        );

        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task SupportGroupMember_ReadsAnotherAccountAndMakesOnlySetupChanges_ForSupportGroup()
    {
        await JoinSupportGroupAsync();

        Assert.True(await OutsiderMayAsync(AuthAction.Read, User.RESOURCE_TYPE));
        Assert.True(await OutsiderMayAsync(AuthAction.Read, IamScenario.INVOICE_RESOURCE_TYPE));
        Assert.True(await OutsiderMayAsync(AuthAction.Create, IamScenario.INVOICE_RESOURCE_TYPE));
        Assert.False(await OutsiderMayAsync(AuthAction.Update, IamScenario.INVOICE_RESOURCE_TYPE));
        Assert.False(await OutsiderMayAsync(AuthAction.Update, User.RESOURCE_TYPE));
        Assert.False(await OutsiderMayAsync(AuthAction.Read, Account.SECRET_KEYS_RESOURCE_TYPE));
    }

    private Task<bool> OutsiderMayAsync(AuthAction action, string resourceType) =>
        _scenario.IsAuthorizedAsync(
            _scenario.OutsiderUsername,
            _scenario.AccountId,
            action,
            resourceType
        );

    private async Task JoinSupportGroupAsync()
    {
        var userId = _scenario.OutsiderUserId;
        var groupId = await _scenario.Host.QueryAsync(db =>
            db.Set<GroupEntity>()
                .Where(g =>
                    g.AccountId == _platformAccountId && g.Name == Group.ACCOUNT_SUPPORT_GROUP_NAME
                )
                .Select(g => g.Id)
                .SingleAsync()
        );
        await _scenario.AsSystemAsync(async services =>
        {
            var registration = services.GetRequiredService<IRegistrationService>();
            await registration.RegisterUserWithAccountAsync(
                new RegisterUserWithAccountRequest(userId, _platformAccountId)
            );
            await registration.RegisterUsersWithGroupAsync(
                new RegisterUsersWithGroupRequest([userId], groupId, _platformAccountId)
            );
            return true;
        });
        await _scenario.EnrollTwoFactorAsync(_scenario.OutsiderUsername);
    }

    [Fact]
    public async Task PlatformMember_EntersAccountTheyDoNotBelongTo_ForAccountRead()
    {
        await GivePlatformRoleAsync((Account.RESOURCE_TYPE, [AuthAction.Read]));

        var current = await _scenario.ActAsAsync(
            _scenario.OutsiderUsername,
            _scenario.AccountId,
            services =>
                Task.FromResult(
                    services
                        .GetRequiredService<Users.Providers.IUserContextProvider>()
                        .GetUserContext()!
                        .CurrentAccount!.Id
                )
        );

        Assert.Equal(_scenario.AccountId, current);
    }

    [Fact]
    public async Task PlatformMember_IsMarkedAsEntering_AnAccountTheyDoNotBelongTo()
    {
        await GivePlatformRoleAsync((Account.RESOURCE_TYPE, [AuthAction.Read]));

        var context = await _scenario.ActAsAsync(
            _scenario.OutsiderUsername,
            _scenario.AccountId,
            services =>
                Task.FromResult(
                    services
                        .GetRequiredService<Users.Providers.IUserContextProvider>()
                        .GetUserContext()!
                )
        );

        Assert.True(context.EnteredAsPlatformMember);
        Assert.DoesNotContain(context.MemberAccounts, a => a.Id == _scenario.AccountId);
        Assert.Contains(context.MemberAccounts, a => a.Id == _platformAccountId);
    }

    [Fact]
    public async Task User_CannotEnterAccountTheyDoNotBelongTo_WithoutPlatformAccountRead()
    {
        var switched = await _scenario.Host.WithScopeAsync(async services =>
        {
            var authentication = services.GetRequiredService<IAuthenticationService>();
            await authentication.SignInAsync(
                new SignInRequest(
                    _scenario.OutsiderUsername,
                    IamScenario.Password,
                    "device-outsider"
                )
            );
            return await authentication.SwitchAccountAsync(
                new SwitchAccountRequest(_scenario.AccountId)
            );
        });

        Assert.NotEqual(SignInResultCode.Success, switched.ResultCode);
    }

    [Fact]
    public async Task PlatformMember_ListsEveryAccount_ForAccountRead()
    {
        await GivePlatformRoleAsync((Account.RESOURCE_TYPE, [AuthAction.Read]));

        var names = await _scenario.ActAsAsync(
            _scenario.OutsiderUsername,
            _platformAccountId,
            async services =>
                (
                    await services
                        .GetRequiredService<IRetrievalService>()
                        .ListAccountsAsync(new ListAccountsRequest(Take: 50))
                ).Data!.Items.Select(a => a.AccountName)
        );

        Assert.Equal(["Platform", "Primary", "Secondary"], names.Order());
    }

    [Fact]
    public async Task PlatformMember_WithOnlySecuritySettings_SavesThemButIsRefusedAuditSettings()
    {
        await GivePlatformRoleAsync(
            (SecurityPolicy.RESOURCE_TYPE, [AuthAction.Read, AuthAction.Update])
        );

        var (security, saved, audit) = await _scenario.ActAsAsync(
            _scenario.OutsiderUsername,
            _platformAccountId,
            async services =>
            {
                var platform = services.GetRequiredService<IPlatformService>();
                var security = await platform.GetSecuritySettingsAsync();
                var saved = await platform.UpdateSecuritySettingsAsync(
                    security.Settings! with
                    {
                        MaxLoginAttempts = 6,
                    }
                );
                var audit = await services
                    .GetRequiredService<IAuditService>()
                    .GetPlatformSettingsAsync();
                return (security, saved, audit);
            }
        );

        Assert.Equal(RetrieveResultCode.Success, security.ResultCode);
        Assert.Equal(ModifyResultCode.Success, saved.ResultCode);
        Assert.Equal(RetrieveResultCode.UnauthorizedError, audit.ResultCode);
    }

    [Fact]
    public async Task PlatformMember_RevealsAKey_AndTheCustomersAuditShowsItWasThem()
    {
        await GivePlatformRoleAsync(
            (Account.RESOURCE_TYPE, [AuthAction.Read]),
            (Account.SECRET_KEYS_RESOURCE_TYPE, [AuthAction.Read, AuthAction.Execute])
        );

        var revealed = await RevealAsOutsiderAsync();

        Assert.Equal(RevealAccountKeyResultCode.Success, revealed.ResultCode);
        var cohort = await _scenario.Host.QueryAsync(db =>
            db.Set<AuditEntryEntity>()
                .Where(e =>
                    e.AccountId == _scenario.AccountId
                    && e.Operation == nameof(IRetrievalService.RevealAccountKeyAsync)
                )
                .Select(e => e.Cohort)
                .SingleAsync()
        );
        Assert.Equal(AuditCohort.PlatformMember, cohort);
    }

    [Fact]
    public async Task PlatformMember_CannotRevealAKey_WithoutThePermission()
    {
        await GivePlatformRoleAsync((Account.RESOURCE_TYPE, [AuthAction.Read]));

        var revealed = await RevealAsOutsiderAsync();

        Assert.Equal(RevealAccountKeyResultCode.UnauthorizedError, revealed.ResultCode);
        Assert.Null(revealed.ReadKey());
    }

    private Task<RevealAccountKeyResult> RevealAsOutsiderAsync() =>
        _scenario.ActAsAsync(
            _scenario.OutsiderUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IRetrievalService>()
                    .RevealAccountKeyAsync(
                        new RevealAccountKeyRequest(
                            _scenario.AccountId,
                            KeyType.SymmetricEncryption,
                            IamScenario.Password,
                            services
                                .GetRequiredService<ITotpProvider>()
                                .GenerateCode(_scenario.TotpSecretOf(_scenario.OutsiderUsername))
                        )
                    )
        );

    [Fact]
    public async Task PlatformMember_HoldsOnlyPlatformRolePermissions_InAnotherAccount()
    {
        await GivePlatformRoleAsync(
            (Account.RESOURCE_TYPE, [AuthAction.Read]),
            (IamScenario.INVOICE_RESOURCE_TYPE, [AuthAction.Read, AuthAction.Create])
        );

        Assert.True(
            await _scenario.IsAuthorizedAsync(
                _scenario.OutsiderUsername,
                _scenario.AccountId,
                AuthAction.Create,
                IamScenario.INVOICE_RESOURCE_TYPE
            )
        );
        Assert.False(
            await _scenario.IsAuthorizedAsync(
                _scenario.OutsiderUsername,
                _scenario.AccountId,
                AuthAction.Delete,
                IamScenario.INVOICE_RESOURCE_TYPE
            )
        );
        Assert.False(
            await _scenario.IsAuthorizedAsync(
                _scenario.OutsiderUsername,
                _scenario.AccountId,
                AuthAction.Read,
                User.RESOURCE_TYPE
            )
        );
    }

    [Fact]
    public async Task PlatformMember_SeesAccessComingFromPlatformAccount_InEffectivePermissions()
    {
        await GivePlatformRoleAsync(
            (Account.RESOURCE_TYPE, [AuthAction.Read]),
            (User.RESOURCE_TYPE, [AuthAction.Read])
        );

        var roles = await _scenario.ActAsAsync(
            _scenario.OutsiderUsername,
            _scenario.AccountId,
            async services =>
                (
                    await services
                        .GetRequiredService<IRetrievalService>()
                        .GetUserAsync(_scenario.OwnerUserId)
                ).EffectivePermissions!.SelectMany(p => p.Roles)
        );

        Assert.Contains(roles, r => r.ViaPlatformAccount);
    }

    [Fact]
    public async Task DeleteAccount_RefusesPlatformAccount_ForSystemContext()
    {
        var result = await _scenario.AsSystemAsync(services =>
            services
                .GetRequiredService<IDeregistrationService>()
                .DeregisterAccountAsync(new DeregisterAccountRequest(_platformAccountId))
        );

        Assert.Equal(DeregisterAccountResultCode.PlatformAccountError, result.ResultCode);
    }

    [Fact]
    public async Task PlatformMember_CannotEnterAccountTheyDoNotBelongTo_WithoutTwoFactor()
    {
        await GivePlatformRoleWithoutTwoFactorAsync((Account.RESOURCE_TYPE, [AuthAction.Read]));

        var switched = await SignInAndSwitchAsync(_scenario.AccountId);

        Assert.Equal(SignInResultCode.TwoFactorRequiredError, switched.ResultCode);
    }

    [Fact]
    public async Task PlatformMember_CannotEnterPlatformAccount_WithoutTwoFactor()
    {
        await GivePlatformRoleWithoutTwoFactorAsync((Account.RESOURCE_TYPE, [AuthAction.Read]));

        var switched = await SignInAndSwitchAsync(_platformAccountId);

        Assert.Equal(SignInResultCode.TwoFactorRequiredError, switched.ResultCode);
    }

    [Fact]
    public async Task PlatformMember_CannotSignInToPlatformAccount_WithoutTwoFactor()
    {
        await GivePlatformRoleWithoutTwoFactorAsync((Account.RESOURCE_TYPE, [AuthAction.Read]));

        var signIn = await _scenario.Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IAuthenticationService>()
                .SignInAsync(
                    new SignInRequest(
                        _scenario.OutsiderUsername,
                        IamScenario.Password,
                        "device-outsider",
                        _platformAccountId
                    )
                )
        );

        Assert.Equal(SignInResultCode.TwoFactorRequiredError, signIn.ResultCode);
    }

    [Fact]
    public async Task PlatformMember_CannotRenewTokenInAnotherAccount_AfterTwoFactorIsTurnedOff()
    {
        await GivePlatformRoleAsync((Account.RESOURCE_TYPE, [AuthAction.Read]));
        var token = await _scenario.ActAsAsync(
            _scenario.OutsiderUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IAuthenticationService>()
                    .SwitchAccountAsync(new SwitchAccountRequest(_scenario.AccountId))
        );
        await _scenario.Host.QueryAsync(db =>
            db.Set<TotpAuthEntity>()
                .Where(t => t.UserId == _scenario.OutsiderUserId)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsEnabled, false))
        );

        var renewed = await _scenario.Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IAuthenticationService>()
                .RenewAuthTokenAsync(new RenewAuthTokenRequest(token.RefreshToken!))
        );

        Assert.Equal(RenewAuthTokenResultCode.TwoFactorRequiredError, renewed.ResultCode);
    }

    [Fact]
    public async Task AccountMember_EntersTheirOwnAccount_WithoutTwoFactor()
    {
        var switched = await _scenario.Host.WithScopeAsync(async services =>
        {
            var authentication = services.GetRequiredService<IAuthenticationService>();
            await authentication.SignInAsync(
                new SignInRequest(
                    _scenario.DirectMemberUsername,
                    IamScenario.Password,
                    "device-member"
                )
            );
            return await authentication.SwitchAccountAsync(
                new SwitchAccountRequest(_scenario.AccountId)
            );
        });

        Assert.Equal(SignInResultCode.Success, switched.ResultCode);
    }

    private Task<SignInResult> SignInAndSwitchAsync(Guid accountId) =>
        _scenario.Host.WithScopeAsync(async services =>
        {
            var authentication = services.GetRequiredService<IAuthenticationService>();
            var signIn = await authentication.SignInAsync(
                new SignInRequest(
                    _scenario.OutsiderUsername,
                    IamScenario.Password,
                    "device-outsider"
                )
            );
            Assert.Equal(SignInResultCode.Success, signIn.ResultCode);
            return await authentication.SwitchAccountAsync(new SwitchAccountRequest(accountId));
        });

    private async Task GivePlatformRoleAsync(
        params (string ResourceType, AuthAction[] Actions)[] grants
    )
    {
        await GivePlatformRoleWithoutTwoFactorAsync(grants);
        await _scenario.EnrollTwoFactorAsync(_scenario.OutsiderUsername);
    }

    private async Task GivePlatformRoleWithoutTwoFactorAsync(
        params (string ResourceType, AuthAction[] Actions)[] grants
    )
    {
        var userId = _scenario.OutsiderUserId;
        await _scenario.AsSystemAsync(async services =>
        {
            var registration = services.GetRequiredService<IRegistrationService>();
            Assert.Equal(
                RegisterUserWithAccountResultCode.Success,
                (
                    await registration.RegisterUserWithAccountAsync(
                        new RegisterUserWithAccountRequest(userId, _platformAccountId)
                    )
                ).ResultCode
            );
            var role = await registration.RegisterRoleAsync(
                new RegisterRoleRequest("Platform slim role", _platformAccountId)
            );
            var permissionIds = new List<Guid>();
            foreach (var (resourceType, actions) in grants)
            {
                permissionIds.Add(
                    await _scenario.PermissionIdAsync(
                        services,
                        _platformAccountId,
                        resourceType,
                        actions
                    )
                );
            }
            await registration.RegisterPermissionsWithRoleAsync(
                new RegisterPermissionsWithRoleRequest(
                    permissionIds,
                    role.CreatedRoleId,
                    _platformAccountId
                )
            );
            await registration.RegisterRolesWithUserAsync(
                new RegisterRolesWithUserRequest([role.CreatedRoleId], userId, _platformAccountId)
            );
            return true;
        });
    }

    private Task<List<string>> SupportRolePermissionsAsync(string roleName) =>
        _scenario.Host.QueryAsync(async db =>
            (
                await db.Set<RoleEntity>()
                    .Where(r =>
                        r.AccountId == _platformAccountId && r.Name == roleName && r.IsSystemDefined
                    )
                    .SelectMany(r => r.Permissions!)
                    .Where(p => p.ResourceId == Guid.Empty && p.IsSystemDefined)
                    .ToListAsync()
            )
                .Select(p =>
                    $"{p.ResourceType}:{(p.Create ? "C" : "")}{(p.Read ? "R" : "")}{(p.Update ? "U" : "")}{(p.Delete ? "D" : "")}{(p.Execute ? "X" : "")}"
                )
                .Order()
                .ToList()
        );
}
