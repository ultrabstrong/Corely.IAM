using Corely.IAM.Accounts.Models;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Platform.Providers;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Web.Components;
using Corely.IAM.Web.Components.ApiKeys;
using Corely.IAM.Web.Components.Shared;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Corely.IAM.Web.UnitTests.Pages.ApiKeys;

public class ApiKeyDetailGroupsTests : BunitContext
{
    private readonly Guid _apiKeyId = Guid.CreateVersion7();
    private readonly Guid _groupId = Guid.CreateVersion7();
    private readonly Guid _roleId = Guid.CreateVersion7();
    private readonly Mock<IBlazorUserContextAccessor> _mockUserContextAccessor = new();
    private readonly Mock<IRetrievalService> _mockRetrievalService = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();

    public ApiKeyDetailGroupsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IIamWebLinks>(new FakeIamWebLinks());
        Services.AddSingleton(_mockUserContextAccessor.Object);
        Services.AddSingleton(_mockRetrievalService.Object);
        Services.AddSingleton(_mockAuthorizationProvider.Object);
        Services.AddSingleton(Mock.Of<IRegistrationService>());
        Services.AddSingleton(Mock.Of<IDeregistrationService>());
        Services.AddSingleton(Mock.Of<IModificationService>());
        Services.AddSingleton<ISecurityPolicyProvider>(
            new FixedSecurityPolicyProvider(SecurityPolicy.Default)
        );
        Services.AddSingleton<ILogger<EntityPageBase>>(NullLogger<EntityPageBase>.Instance);
        ComponentFactories.AddStub<ApiKeyAccessPanel>();

        _mockUserContextAccessor
            .Setup(x => x.GetUserContextAsync())
            .ReturnsAsync(
                PageTestHelpers.CreateUserContext(
                    currentAccount: new Account { Id = Guid.CreateVersion7() }
                )
            );
        _mockRetrievalService
            .Setup(x => x.GetApiKeyAsync(_apiKeyId, true))
            .ReturnsAsync(
                new RetrieveSingleResult<ApiKey>(
                    RetrieveResultCode.Success,
                    string.Empty,
                    new ApiKey
                    {
                        Id = _apiKeyId,
                        Name = "importer",
                        Secrets = [],
                        Roles = [new ChildRef(_roleId, "Editors")],
                        Groups = [new ChildRef(_groupId, "Finance")],
                    },
                    null
                )
            );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<Guid[]>())
            )
            .ReturnsAsync(true);
    }

    private void DenyUpdateOnGroups() =>
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, Group.RESOURCE_TYPE, It.IsAny<Guid[]>())
            )
            .ReturnsAsync(false);

    private IRenderedComponent<ApiKeyDetail> RenderPage()
    {
        var cut = Render<ApiKeyDetail>(p => p.Add(x => x.Id, _apiKeyId));
        cut.WaitForAssertion(() => Assert.Contains("Finance", cut.Markup));
        cut.WaitForAssertion(() =>
            Assert.Contains(cut.FindAll("button"), b => b.TextContent.Trim() == "Add Roles")
        );
        return cut;
    }

    [Fact]
    public void GroupActions_Show_ForACallerWhoCanUpdateGroups()
    {
        var cut = RenderPage();

        Assert.Contains(cut.FindAll("button"), b => b.TextContent.Trim() == "Add to Groups");
        Assert.Equal(2, cut.FindAll("button[title='Remove']").Count);
    }

    [Fact]
    public void GroupActions_Hide_ForACallerWhoCanUpdateTheKeyButNotGroups()
    {
        DenyUpdateOnGroups();

        var cut = RenderPage();

        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Add to Groups");
        Assert.Single(cut.FindAll("button[title='Remove']"));
    }

    [Fact]
    public void RoleRemoval_StaysGatedOnTheKey()
    {
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Update, ApiKey.RESOURCE_TYPE, It.IsAny<Guid[]>())
            )
            .ReturnsAsync(false);

        var cut = Render<ApiKeyDetail>(p => p.Add(x => x.Id, _apiKeyId));
        cut.WaitForAssertion(() =>
            Assert.Contains(cut.FindAll("button"), b => b.TextContent.Trim() == "Add to Groups")
        );

        Assert.Single(cut.FindAll("button[title='Remove']"));
    }

    [Fact]
    public void GroupPicker_OffersOnlyGroupsTheCallerCanUpdate()
    {
        var allowed = new Group { Id = Guid.CreateVersion7(), Name = "allowed" };
        var forbidden = new Group { Id = Guid.CreateVersion7(), Name = "forbidden" };
        _mockRetrievalService
            .Setup(x => x.ListGroupsAsync(It.IsAny<ListGroupsRequest>()))
            .ReturnsAsync(
                new RetrieveListResult<Group>(
                    RetrieveResultCode.Success,
                    string.Empty,
                    PagedResult<Group>.Create([allowed, forbidden], 2, 0, 25)
                )
            );
        _mockAuthorizationProvider
            .Setup(x => x.GetGrantableGroupIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new HashSet<Guid> { allowed.Id, forbidden.Id });
        _mockAuthorizationProvider
            .Setup(x => x.GetAuthorizedResourceIdsAsync(AuthAction.Update, Group.RESOURCE_TYPE))
            .ReturnsAsync(new HashSet<Guid> { allowed.Id });

        var cut = RenderPage();
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Add to Groups").Click();

        cut.WaitForAssertion(() =>
            Assert.Equal(["allowed"], cut.FindAll(".picker-item-text").Select(e => e.TextContent))
        );
    }
}
