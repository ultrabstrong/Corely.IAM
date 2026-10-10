using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Audits.Constants;
using Corely.IAM.Audits.Entities;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;
using Corely.IAM.Services;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corely.IAM.UnitTests.Audits.Providers;

public class AuditProviderTests
{
    private const string SERVICE = nameof(IRegistrationService);
    private const string SOURCE = "unit-tests";

    private readonly ServiceFactory _serviceFactory = new();
    private readonly FakeUserContextProvider _userContextProvider = new();
    private readonly Mock<IAuditPolicy> _policy = new();
    private readonly Mock<IRepo<AuditEntryEntity>> _entryRepo = new();
    private readonly Mock<ILogger<AuditProvider>> _logger = new();
    private readonly List<AuditEntryEntity> _written = [];
    private readonly AuditProvider _provider;

    private readonly Guid _accountId = Guid.CreateVersion7();
    private readonly Guid _memberId = Guid.CreateVersion7();
    private readonly Guid _outsiderId = Guid.CreateVersion7();

    public AuditProviderTests()
    {
        _entryRepo
            .Setup(r => r.CreateAsync(It.IsAny<AuditEntryEntity>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEntryEntity, CancellationToken>((e, _) => _written.Add(e))
            .ReturnsAsync((AuditEntryEntity e, CancellationToken _) => e);

        _policy
            .Setup(p =>
                p.IsRecordedAsync(
                    It.IsAny<AuditCohort>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<AuthAction>()
                )
            )
            .ReturnsAsync(true);

        var scopedServices = new Mock<IServiceProvider>();
        scopedServices
            .Setup(s => s.GetService(typeof(IRepo<AuditEntryEntity>)))
            .Returns(_entryRepo.Object);
        scopedServices.Setup(s => s.GetService(typeof(IAuditPolicy))).Returns(_policy.Object);
        scopedServices
            .Setup(s => s.GetService(typeof(IReadonlyRepo<AccountEntity>)))
            .Returns(_serviceFactory.GetRequiredService<IReadonlyRepo<AccountEntity>>());
        scopedServices
            .Setup(s => s.GetService(typeof(IReadonlyRepo<UserEntity>)))
            .Returns(_serviceFactory.GetRequiredService<IReadonlyRepo<UserEntity>>());
        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(scopedServices.Object);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        _provider = new AuditProvider(
            _userContextProvider,
            scopeFactory.Object,
            TimeProvider.System,
            Options.Create(new AuditOptions { Source = SOURCE }),
            _logger.Object
        );
    }

    [Fact]
    public async Task Record_WritesTheCall_ForAnAccountMember()
    {
        await SeedAccountAsync();
        _userContextProvider.Context = UserIn(_memberId, _accountId);
        var groupId = Guid.CreateVersion7();

        var result = await _provider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Create, PermissionConstants.GROUP_RESOURCE_TYPE)
            {
                AccountId = _accountId,
            },
            () => Task.FromResult(new ModifyResult(ModifyResultCode.Success, "")),
            r => AuditOutcome.Of(r.ResultCode, groupId),
            "RegisterGroupAsync"
        );

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        var entry = Assert.Single(_written);
        Assert.Equal(_memberId, entry.ActorUserId);
        Assert.Equal(AuditCohort.AccountMember, entry.Cohort);
        Assert.Equal(_accountId, entry.AccountId);
        Assert.Equal(SOURCE, entry.Source);
        Assert.Equal(SERVICE, entry.Service);
        Assert.Equal("RegisterGroupAsync", entry.Operation);
        Assert.Equal(AuthAction.Create, entry.Action);
        Assert.Equal(PermissionConstants.GROUP_RESOURCE_TYPE, entry.ResourceType);
        Assert.Equal(groupId.ToString(), entry.ResourceIds);
        Assert.Equal(nameof(ModifyResultCode.Success), entry.ResultCode);
        Assert.Null(entry.Details);
        Assert.NotEqual(Guid.Empty, entry.Id);
    }

    [Fact]
    public async Task Record_UsesTheOperationName_FromTheCaller()
    {
        await RecordAccountlessAsync();

        Assert.Equal(nameof(RecordAccountlessAsync), Assert.Single(_written).Operation);
    }

    [Fact]
    public async Task Record_WritesNothing_WhenThePolicyDeclines()
    {
        _policy
            .Setup(p =>
                p.IsRecordedAsync(
                    It.IsAny<AuditCohort>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<AuthAction>()
                )
            )
            .ReturnsAsync(false);

        var result = await RecordAccountlessAsync();

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        Assert.Empty(_written);
    }

    [Fact]
    public async Task Record_WritesThePlatformMemberCohort_WhenTheActorIsNotAMember()
    {
        await SeedAccountAsync();
        _userContextProvider.Context = UserIn(_outsiderId, _accountId);

        await RecordInAccountAsync();

        Assert.Equal(AuditCohort.PlatformMember, Assert.Single(_written).Cohort);
    }

    [Fact]
    public async Task Record_KeepsTheMemberCohort_WhenTheCallRemovesTheMembership()
    {
        var account = await SeedAccountAsync();
        _userContextProvider.Context = UserIn(_memberId, _accountId);

        await _provider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, PermissionConstants.ACCOUNT_RESOURCE_TYPE)
            {
                AccountId = _accountId,
            },
            () =>
            {
                account.Users!.Clear();
                return Task.FromResult(new ModifyResult(ModifyResultCode.Success, ""));
            },
            r => AuditOutcome.Of(r.ResultCode)
        );

        Assert.Equal(AuditCohort.AccountMember, Assert.Single(_written).Cohort);
    }

    [Fact]
    public async Task Record_WritesTheMemberCohort_WhenTheCallCreatesTheMembership()
    {
        var account = await SeedAccountAsync(withMember: false);
        _userContextProvider.Context = UserIn(_memberId, null);

        await _provider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Create, PermissionConstants.ACCOUNT_RESOURCE_TYPE)
            {
                AccountId = _accountId,
            },
            () =>
            {
                account.Users!.Add(new UserEntity { Id = _memberId, Username = "member" });
                return Task.FromResult(new ModifyResult(ModifyResultCode.Success, ""));
            },
            r => AuditOutcome.Of(r.ResultCode)
        );

        Assert.Equal(AuditCohort.AccountMember, Assert.Single(_written).Cohort);
    }

    [Fact]
    public async Task Record_SkipsTheMembershipQuery_WhenNeitherAccountCohortRecordsTheAction()
    {
        _policy
            .Setup(p =>
                p.IsRecordedAsync(
                    It.IsAny<AuditCohort>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<AuthAction>()
                )
            )
            .ReturnsAsync(false);
        _userContextProvider.Context = UserIn(_memberId, _accountId);

        await RecordInAccountAsync();

        Assert.Empty(_written);
        _policy.Verify(
            p => p.IsRecordedAsync(AuditCohort.AccountMember, _accountId, AuthAction.Update),
            Times.AtLeastOnce
        );
        _policy.Verify(
            p => p.IsRecordedAsync(AuditCohort.PlatformMember, _accountId, AuthAction.Update),
            Times.AtLeastOnce
        );
    }

    [Fact]
    public async Task Record_WritesTheSystemContextCohort_WithNoActor()
    {
        _userContextProvider.Context = new UserContext(true, "system");

        await RecordInAccountAsync();

        var entry = Assert.Single(_written);
        Assert.Equal(AuditCohort.SystemContext, entry.Cohort);
        Assert.Null(entry.ActorUserId);
        Assert.Equal(_accountId, entry.AccountId);
    }

    [Fact]
    public async Task Record_WritesNoAccount_ForAnAccountlessCall()
    {
        _userContextProvider.Context = UserIn(_memberId, _accountId);

        await RecordAccountlessAsync();

        var entry = Assert.Single(_written);
        Assert.Null(entry.AccountId);
        Assert.Equal(AuditCohort.Accountless, entry.Cohort);
        Assert.Equal(_memberId, entry.ActorUserId);
    }

    [Fact]
    public async Task Record_UsesTheCurrentAccount_WhenTheCallNamesNone()
    {
        await SeedAccountAsync();
        _userContextProvider.Context = UserIn(_memberId, _accountId);

        await _provider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, PermissionConstants.USER_RESOURCE_TYPE),
            () => Task.FromResult(new ModifyResult(ModifyResultCode.Success, "")),
            r => AuditOutcome.Of(r.ResultCode)
        );

        Assert.Equal(_accountId, Assert.Single(_written).AccountId);
    }

    [Fact]
    public async Task Record_UsesTheContextTheCallSets_WhenThereWasNoneBefore()
    {
        await SeedAccountAsync();

        await _provider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Execute, PermissionConstants.USER_RESOURCE_TYPE),
            () =>
            {
                _userContextProvider.Context = UserIn(_memberId, _accountId);
                return Task.FromResult(new ModifyResult(ModifyResultCode.Success, ""));
            },
            r => AuditOutcome.Of(r.ResultCode)
        );

        var entry = Assert.Single(_written);
        Assert.Equal(_memberId, entry.ActorUserId);
        Assert.Equal(_accountId, entry.AccountId);
        Assert.Equal(AuditCohort.AccountMember, entry.Cohort);
    }

    [Fact]
    public async Task Record_FindsTheActorByUsername_WhenThereIsNoContext()
    {
        await _serviceFactory
            .GetRequiredService<IRepo<UserEntity>>()
            .CreateAsync(new UserEntity { Id = _memberId, Username = "member" });

        await _provider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Execute, PermissionConstants.USER_RESOURCE_TYPE)
            {
                AccountId = Guid.Empty,
                ActorUsername = "member",
            },
            () => Task.FromResult(new ModifyResult(ModifyResultCode.UnauthorizedError, "")),
            r => AuditOutcome.Of(r.ResultCode)
        );

        var entry = Assert.Single(_written);
        Assert.Equal(_memberId, entry.ActorUserId);
        Assert.Equal(nameof(ModifyResultCode.UnauthorizedError), entry.ResultCode);
    }

    [Fact]
    public async Task Record_WritesNoActor_ForAnUnknownUsername()
    {
        await _provider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Execute, PermissionConstants.USER_RESOURCE_TYPE)
            {
                AccountId = Guid.Empty,
                ActorUsername = "nobody",
            },
            () => Task.FromResult(new ModifyResult(ModifyResultCode.NotFoundError, "")),
            r => AuditOutcome.Of(r.ResultCode)
        );

        Assert.Null(Assert.Single(_written).ActorUserId);
    }

    [Fact]
    public async Task Record_UsesTheOutcomesActor_WhenNoContextHasOne()
    {
        var createdUserId = Guid.CreateVersion7();

        await _provider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Create, PermissionConstants.USER_RESOURCE_TYPE)
            {
                AccountId = Guid.Empty,
            },
            () => Task.FromResult(createdUserId),
            id => AuditOutcome.Of(RegisterUserResultCode.Success, id) with { ActorUserId = id }
        );

        var entry = Assert.Single(_written);
        Assert.Equal(createdUserId, entry.ActorUserId);
        Assert.Equal(createdUserId.ToString(), entry.ResourceIds);
    }

    [Fact]
    public async Task Record_MergesRequestAndResultIds_AndDropsEmptyOnes()
    {
        var requestId = Guid.CreateVersion7();
        var resultId = Guid.CreateVersion7();

        await _provider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE)
            {
                AccountId = Guid.Empty,
                ResourceIds = [requestId, Guid.Empty],
            },
            () => Task.FromResult(new ModifyResult(ModifyResultCode.Success, "")),
            r => AuditOutcome.Of(r.ResultCode, resultId, Guid.Empty, requestId)
        );

        Assert.Equal($"{requestId},{resultId}", Assert.Single(_written).ResourceIds);
    }

    [Fact]
    public async Task Record_WritesAFault_AndRethrows_WhenTheCallThrows()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _provider.RecordAsync<ModifyResult>(
                new AuditCall(SERVICE, AuthAction.Delete, PermissionConstants.GROUP_RESOURCE_TYPE)
                {
                    AccountId = Guid.Empty,
                },
                () => throw new InvalidOperationException("boom"),
                r => AuditOutcome.Of(r.ResultCode)
            )
        );

        Assert.Equal("boom", exception.Message);
        Assert.Equal(AuditConstants.FAULT_RESULT_CODE, Assert.Single(_written).ResultCode);
    }

    [Fact]
    public async Task Record_ReturnsTheResult_AndLogsAnError_WhenTheWriteFails()
    {
        _entryRepo
            .Setup(r => r.CreateAsync(It.IsAny<AuditEntryEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database down"));

        var result = await RecordAccountlessAsync();

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        VerifyErrorLogged();
    }

    [Fact]
    public async Task Record_ReturnsTheResult_AndLogsAnError_WhenTheOutcomeCannotBeRead()
    {
        var result = await _provider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE)
            {
                AccountId = Guid.Empty,
            },
            () => Task.FromResult(new ModifyResult(ModifyResultCode.Success, "")),
            _ => throw new InvalidOperationException("bad outcome")
        );

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        Assert.Empty(_written);
        VerifyErrorLogged();
    }

    [Fact]
    public async Task Record_WritesWithoutACancellationToken()
    {
        await RecordAccountlessAsync();

        _entryRepo.Verify(
            r => r.CreateAsync(It.IsAny<AuditEntryEntity>(), CancellationToken.None),
            Times.Once
        );
    }

    [Fact]
    public async Task Record_WritesAlwaysRecordedOperations_WhenThePolicyDeclines()
    {
        _policy
            .Setup(p =>
                p.IsRecordedAsync(
                    It.IsAny<AuditCohort>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<AuthAction>()
                )
            )
            .ReturnsAsync(false);
        _policy
            .Setup(p =>
                p.IsAlwaysRecorded(
                    nameof(IDeregistrationService),
                    nameof(IDeregistrationService.DeregisterUserAsync)
                )
            )
            .Returns(true);
        _userContextProvider.Context = UserIn(_memberId, null, "member-name");

        await _provider.RecordAsync(
            new AuditCall(
                nameof(IDeregistrationService),
                AuthAction.Delete,
                PermissionConstants.USER_RESOURCE_TYPE
            )
            {
                AccountId = Guid.Empty,
                Detail = AuditDetail.DeletedUsername,
            },
            () =>
            {
                _userContextProvider.Context = null;
                return Task.FromResult(
                    new DeregisterUserResult(DeregisterUserResultCode.Success, "")
                );
            },
            r => AuditOutcome.Of(r.ResultCode),
            nameof(IDeregistrationService.DeregisterUserAsync)
        );

        var entry = Assert.Single(_written);
        Assert.Equal("member-name", entry.Details);
        Assert.Equal(_memberId, entry.ActorUserId);
        Assert.Equal(_memberId.ToString(), entry.ResourceIds);
    }

    [Fact]
    public async Task Record_WritesTheAccountName_ForADeletedAccount()
    {
        _policy
            .Setup(p =>
                p.IsAlwaysRecorded(
                    nameof(IDeregistrationService),
                    nameof(IDeregistrationService.DeregisterAccountAsync)
                )
            )
            .Returns(true);
        await SeedAccountAsync();
        _userContextProvider.Context = UserIn(_memberId, _accountId);

        await _provider.RecordAsync(
            new AuditCall(
                nameof(IDeregistrationService),
                AuthAction.Delete,
                PermissionConstants.ACCOUNT_RESOURCE_TYPE
            )
            {
                AccountId = _accountId,
                ResourceIds = [_accountId],
                Detail = AuditDetail.DeletedAccountName,
            },
            () =>
                Task.FromResult(
                    new DeregisterAccountResult(DeregisterAccountResultCode.Success, "")
                ),
            r => AuditOutcome.Of(r.ResultCode),
            nameof(IDeregistrationService.DeregisterAccountAsync)
        );

        var entry = Assert.Single(_written);
        Assert.Equal("Acme", entry.Details);
        Assert.Equal(AuditCohort.AccountMember, entry.Cohort);
    }

    [Fact]
    public async Task Record_WritesSuccess_ForAnOperationWithoutAResult()
    {
        await _provider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, PermissionConstants.USER_RESOURCE_TYPE)
            {
                AccountId = Guid.Empty,
            },
            () => Task.CompletedTask
        );

        Assert.Equal(AuditConstants.SUCCESS_RESULT_CODE, Assert.Single(_written).ResultCode);
    }

    private Task<ModifyResult> RecordAccountlessAsync() =>
        _provider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, PermissionConstants.USER_RESOURCE_TYPE)
            {
                AccountId = Guid.Empty,
            },
            () => Task.FromResult(new ModifyResult(ModifyResultCode.Success, "")),
            r => AuditOutcome.Of(r.ResultCode)
        );

    private Task<ModifyResult> RecordInAccountAsync() =>
        _provider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE)
            {
                AccountId = _accountId,
            },
            () => Task.FromResult(new ModifyResult(ModifyResultCode.Success, "")),
            r => AuditOutcome.Of(r.ResultCode)
        );

    private async Task<AccountEntity> SeedAccountAsync(bool withMember = true)
    {
        var account = new AccountEntity
        {
            Id = _accountId,
            AccountName = "Acme",
            Users = withMember ? [new UserEntity { Id = _memberId, Username = "member" }] : [],
        };
        await _serviceFactory.GetRequiredService<IRepo<AccountEntity>>().CreateAsync(account);
        return account;
    }

    private static UserContext UserIn(Guid userId, Guid? accountId, string username = "user") =>
        new(
            new User { Id = userId, Username = username },
            accountId is { } id ? new Account { Id = id, AccountName = "Acme" } : null,
            "device",
            []
        );

    private void VerifyErrorLogged() =>
        _logger.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );

    private sealed class FakeUserContextProvider : IUserContextProvider
    {
        public UserContext? Context { get; set; }

        public UserContext? GetUserContext() => Context;
    }
}
