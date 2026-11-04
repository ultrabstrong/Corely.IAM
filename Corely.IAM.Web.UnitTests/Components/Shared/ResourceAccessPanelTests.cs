using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Web.Components.Shared;
using Corely.IAM.Web.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.Web.UnitTests.Components.Shared;

public class ResourceAccessPanelTests : BunitContext
{
    private const string INVOICE = "invoice";

    private readonly Mock<IRetrievalService> _retrieval = new();

    public ResourceAccessPanelTests()
    {
        var accountId = Guid.CreateVersion7();
        var account = new Account { Id = accountId, AccountName = "Acme" };
        var accessor = new Mock<IBlazorUserContextAccessor>();
        accessor
            .Setup(x => x.GetUserContextAsync())
            .ReturnsAsync(
                new UserContext(
                    new User { Id = Guid.CreateVersion7(), Username = "viewer" },
                    account,
                    "device",
                    [account]
                )
            );
        Services.AddSingleton(accessor.Object);
        Services.AddSingleton(_retrieval.Object);
        Services.AddSingleton(Mock.Of<IIamWebLinks>());
    }

    private static List<EffectivePermission> Granted() =>
        [
            new EffectivePermission
            {
                PermissionId = Guid.CreateVersion7(),
                Read = true,
                ResourceType = INVOICE,
                Roles =
                [
                    new EffectiveRole
                    {
                        RoleId = Guid.CreateVersion7(),
                        RoleName = "Invoice reader",
                        IsDirect = true,
                    },
                ],
            },
        ];

    private void Answer(RetrieveResultCode code, List<EffectivePermission>? permissions) =>
        _retrieval
            .Setup(x =>
                x.GetCurrentUserEffectivePermissionsAsync(It.IsAny<string>(), It.IsAny<Guid?>())
            )
            .ReturnsAsync(
                new RetrieveSingleResult<List<EffectivePermission>>(code, "", permissions, null)
            );

    [Fact]
    public void Panel_ShowsTheSectionTheOtherPagesUse_ForAHostsResource()
    {
        var invoiceId = Guid.CreateVersion7();
        Answer(RetrieveResultCode.Success, Granted());

        var cut = Render<ResourceAccessPanel>(p =>
            p.Add(x => x.ResourceType, INVOICE).Add(x => x.ResourceId, invoiceId)
        );

        Assert.Equal("Your Access", cut.Find(".iam-drawer-header .card-section-title").TextContent);
        Assert.Contains("Invoice reader", cut.Markup);
        _retrieval.Verify(x => x.GetCurrentUserEffectivePermissionsAsync(INVOICE, invoiceId));
    }

    [Fact]
    public void Panel_AsksForTheTypeAsAWhole_WithoutAResourceId()
    {
        Answer(RetrieveResultCode.Success, Granted());

        Render<ResourceAccessPanel>(p => p.Add(x => x.ResourceType, INVOICE));

        _retrieval.Verify(x => x.GetCurrentUserEffectivePermissionsAsync(INVOICE, null));
    }

    [Theory]
    [InlineData(RetrieveResultCode.UnauthorizedError)]
    [InlineData(RetrieveResultCode.NotFoundError)]
    public void Panel_ShowsNothing_ForARefusal(RetrieveResultCode code)
    {
        Answer(code, Granted());

        var cut = Render<ResourceAccessPanel>(p => p.Add(x => x.ResourceType, INVOICE));

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void Panel_ShowsNothing_ForAUserWithNoAccess()
    {
        Answer(RetrieveResultCode.Success, []);

        var cut = Render<ResourceAccessPanel>(p => p.Add(x => x.ResourceType, INVOICE));

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void Panel_ShowsNothing_WhenTheLoadThrows()
    {
        _retrieval
            .Setup(x =>
                x.GetCurrentUserEffectivePermissionsAsync(It.IsAny<string>(), It.IsAny<Guid?>())
            )
            .ThrowsAsync(new InvalidOperationException("database is down"));

        var cut = Render<ResourceAccessPanel>(p => p.Add(x => x.ResourceType, INVOICE));

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void Panel_Reloads_WhenTheResourceChanges()
    {
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();
        Answer(RetrieveResultCode.Success, Granted());
        var cut = Render<ResourceAccessPanel>(p =>
            p.Add(x => x.ResourceType, INVOICE).Add(x => x.ResourceId, first)
        );

        cut.Render(p => p.Add(x => x.ResourceId, second));

        _retrieval.Verify(
            x => x.GetCurrentUserEffectivePermissionsAsync(INVOICE, second),
            Times.Once
        );
    }
}
