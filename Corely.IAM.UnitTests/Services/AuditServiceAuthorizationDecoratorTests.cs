using Corely.IAM.Audits.Constants;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;

namespace Corely.IAM.UnitTests.Services;

public class AuditServiceAuthorizationDecoratorTests
{
    private readonly Mock<IAuditService> _mockInnerService = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly Mock<IAuditAccessProvider> _mockAccessProvider = new();
    private readonly AuditServiceAuthorizationDecorator _decorator;
    private readonly Guid _accountId = Guid.CreateVersion7();

    public AuditServiceAuthorizationDecoratorTests()
    {
        _decorator = new AuditServiceAuthorizationDecorator(
            _mockInnerService.Object,
            _mockAuthorizationProvider.Object,
            _mockAccessProvider.Object
        );
    }

    [Fact]
    public async Task ListEntries_DelegatesToInner_WhenThereIsAUserContext()
    {
        var request = new ListAuditEntriesRequest();
        var expected = new RetrieveListResult<AuditEntry>(
            RetrieveResultCode.Success,
            "",
            PagedResult<AuditEntry>.Empty()
        );
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(true);
        _mockInnerService.Setup(x => x.ListEntriesAsync(request)).ReturnsAsync(expected);

        Assert.Equal(expected, await _decorator.ListEntriesAsync(request));
    }

    [Fact]
    public async Task ListEntries_IsRefused_WithoutAUserContext()
    {
        var result = await _decorator.ListEntriesAsync(new ListAuditEntriesRequest());

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.ListEntriesAsync(It.IsAny<ListAuditEntriesRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ExportEntries_IsRefused_WithoutAUserContext()
    {
        var result = await _decorator.ExportEntriesAsync(new AuditEntryFilter());

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        Assert.Null(result.Csv);
    }

    [Fact]
    public async Task Purge_DelegatesToInner_WhenTheViewerHoldsDeleteInTheAccount()
    {
        var request = new PurgeAuditEntriesRequest(_accountId);
        SetDeleteAccess(new AuditAccess(false, new HashSet<Guid> { _accountId }, Guid.Empty));
        _mockInnerService
            .Setup(x => x.PurgeEntriesAsync(request))
            .ReturnsAsync(new PurgeAuditEntriesResult(PurgeAuditEntriesResultCode.Success, "", 3));

        var result = await _decorator.PurgeEntriesAsync(request);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task Purge_IsRefused_ForAnAccountTheViewerCannotDeleteIn()
    {
        SetDeleteAccess(new AuditAccess(false, new HashSet<Guid> { _accountId }, Guid.Empty));

        var result = await _decorator.PurgeEntriesAsync(
            new PurgeAuditEntriesRequest(Guid.CreateVersion7())
        );

        Assert.Equal(PurgeAuditEntriesResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerService.Verify(
            x => x.PurgeEntriesAsync(It.IsAny<PurgeAuditEntriesRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Purge_OfEntriesOutsideAnyAccount_NeedsAccessToEverything()
    {
        SetDeleteAccess(new AuditAccess(false, new HashSet<Guid> { _accountId }, Guid.Empty));

        var refused = await _decorator.CountPurgeableEntriesAsync(
            new PurgeAuditEntriesRequest(null)
        );

        Assert.Equal(PurgeAuditEntriesResultCode.UnauthorizedError, refused.ResultCode);

        SetDeleteAccess(AuditAccess.All);
        _mockInnerService
            .Setup(x => x.CountPurgeableEntriesAsync(It.IsAny<PurgeAuditEntriesRequest>()))
            .ReturnsAsync(new PurgeAuditEntriesResult(PurgeAuditEntriesResultCode.Success, "", 9));

        var allowed = await _decorator.CountPurgeableEntriesAsync(
            new PurgeAuditEntriesRequest(null)
        );

        Assert.Equal(9, allowed.Count);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public async Task UpdateAccountSettings_NeedsAccountContextAndUpdateOnAuditSettings(
        bool hasAccountContext,
        bool isAuthorized,
        bool expectedAllowed
    )
    {
        var request = new UpdateAccountAuditSettingsRequest(
            _accountId,
            AuditActions.All,
            AuditActions.None,
            30
        );
        _mockAuthorizationProvider
            .Setup(x => x.HasAccountContext(_accountId))
            .Returns(hasAccountContext);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, AuditConstants.AUDIT_SETTINGS_RESOURCE_TYPE)
            )
            .ReturnsAsync(isAuthorized);
        _mockInnerService
            .Setup(x => x.UpdateAccountSettingsAsync(request))
            .ReturnsAsync(new ModifyResult(ModifyResultCode.Success, ""));

        var result = await _decorator.UpdateAccountSettingsAsync(request);

        Assert.Equal(
            expectedAllowed ? ModifyResultCode.Success : ModifyResultCode.UnauthorizedError,
            result.ResultCode
        );
    }

    [Fact]
    public async Task GetAccountSettings_ChecksReadOnAuditSettings()
    {
        _mockAuthorizationProvider.Setup(x => x.HasAccountContext(_accountId)).Returns(true);
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, AuditConstants.AUDIT_SETTINGS_RESOURCE_TYPE)
            )
            .ReturnsAsync(false);

        var result = await _decorator.GetAccountSettingsAsync(_accountId);

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
    }

    [Theory]
    [InlineData(true, ModifyResultCode.Success)]
    [InlineData(false, ModifyResultCode.UnauthorizedError)]
    public async Task UpdatePlatformSettings_NeedsUpdateOnPlatformSettingsInThePlatformAccount(
        bool holds,
        ModifyResultCode expected
    )
    {
        _mockAccessProvider
            .Setup(x =>
                x.HoldsInPlatformAccountAsync(
                    AuthAction.Update,
                    AuditConstants.PLATFORM_SETTINGS_RESOURCE_TYPE
                )
            )
            .ReturnsAsync(holds);
        _mockInnerService
            .Setup(x => x.UpdatePlatformSettingsAsync(It.IsAny<PlatformSettings>()))
            .ReturnsAsync(new ModifyResult(ModifyResultCode.Success, ""));

        var result = await _decorator.UpdatePlatformSettingsAsync(PlatformSettings.Default);

        Assert.Equal(expected, result.ResultCode);
    }

    [Fact]
    public async Task GetPlatformSettings_IsRefused_WithoutReadInThePlatformAccount()
    {
        var result = await _decorator.GetPlatformSettingsAsync();

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
    }

    [Theory]
    [InlineData(true, false, DeleteExpiredAuditEntriesResultCode.Success)]
    [InlineData(true, true, DeleteExpiredAuditEntriesResultCode.UnauthorizedError)]
    [InlineData(false, false, DeleteExpiredAuditEntriesResultCode.UnauthorizedError)]
    public async Task DeleteExpiredEntries_RunsOnlyUnderSystemContext(
        bool hasUserContext,
        bool isNonSystemUser,
        DeleteExpiredAuditEntriesResultCode expected
    )
    {
        _mockAuthorizationProvider.Setup(x => x.HasUserContext()).Returns(hasUserContext);
        _mockAuthorizationProvider.Setup(x => x.IsNonSystemUserContext()).Returns(isNonSystemUser);
        _mockInnerService
            .Setup(x => x.DeleteExpiredEntriesAsync())
            .ReturnsAsync(
                new DeleteExpiredAuditEntriesResult(
                    DeleteExpiredAuditEntriesResultCode.Success,
                    "",
                    0
                )
            );

        var result = await _decorator.DeleteExpiredEntriesAsync();

        Assert.Equal(expected, result.ResultCode);
    }

    private void SetDeleteAccess(AuditAccess access) =>
        _mockAccessProvider.Setup(x => x.GetAccessAsync(AuthAction.Delete)).ReturnsAsync(access);
}
