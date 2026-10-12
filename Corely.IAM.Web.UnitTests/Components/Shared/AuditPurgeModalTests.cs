using Corely.IAM.Audits.Models;
using Corely.IAM.Models;
using Corely.IAM.Security.Constants;
using Corely.IAM.Services;
using Corely.IAM.Web.Components.Shared;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using TestContext = Bunit.TestContext;

namespace Corely.IAM.Web.UnitTests.Components.Shared;

public class AuditPurgeModalTests : TestContext
{
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Guid _deletedAccountId = Guid.CreateVersion7();

    public AuditPurgeModalTests()
    {
        var userContextAccessor = new Mock<IBlazorUserContextAccessor>();
        userContextAccessor
            .Setup(x => x.GetUserContextAsync())
            .ReturnsAsync(PageTestHelpers.CreateUserContext());
        Services.AddSingleton(userContextAccessor.Object);
        Services.AddSingleton(_auditService.Object);
        _auditService
            .Setup(x => x.ListAuditAccountsAsync(AuthAction.Delete))
            .ReturnsAsync(
                new ListAuditAccountsResult(
                    RetrieveResultCode.Success,
                    "",
                    [new AuditAccountOption(Guid.CreateVersion7(), "Live Co")],
                    true,
                    [new AuditAccountOption(_deletedAccountId, "Gone Inc")]
                )
            );
    }

    [Fact]
    public async Task Show_GroupsDeletedAccountsApart()
    {
        var cut = Render<AuditPurgeModal>();

        await cut.InvokeAsync(() => cut.Instance.ShowAsync());

        var group = cut.Find("optgroup[label='Deleted accounts']");
        Assert.Contains("Gone Inc", group.TextContent);
        Assert.DoesNotContain("Live Co", group.TextContent);
    }

    [Fact]
    public async Task Review_CountsTheDeletedAccountsEntries()
    {
        _auditService
            .Setup(x => x.CountPurgeableEntriesAsync(It.IsAny<PurgeAuditEntriesRequest>()))
            .ReturnsAsync(new PurgeAuditEntriesResult(PurgeAuditEntriesResultCode.Success, "", 4));
        var cut = Render<AuditPurgeModal>();
        await cut.InvokeAsync(() => cut.Instance.ShowAsync());

        cut.Find("select").Change(_deletedAccountId.ToString());
        cut.FindAll("button").First(b => b.TextContent == "Review").Click();

        _auditService.Verify(
            x =>
                x.CountPurgeableEntriesAsync(new PurgeAuditEntriesRequest(_deletedAccountId, null)),
            Times.Once
        );
        cut.WaitForAssertion(() => Assert.Contains("Purge 4 entries", cut.Markup));
    }
}
