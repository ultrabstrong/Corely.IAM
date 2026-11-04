using Corely.IAM.Accounts.Models;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Platform.Providers;
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

public class ApiKeyListTests : BunitContext
{
    private readonly Mock<IBlazorUserContextAccessor> _mockUserContextAccessor = new();
    private readonly Mock<IRetrievalService> _mockRetrievalService = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();

    public ApiKeyListTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IIamWebLinks>(new FakeIamWebLinks());
        Services.AddSingleton(_mockUserContextAccessor.Object);
        Services.AddSingleton(_mockRetrievalService.Object);
        Services.AddSingleton(_mockAuthorizationProvider.Object);
        Services.AddSingleton(Mock.Of<IRegistrationService>());
        Services.AddSingleton(Mock.Of<IDeregistrationService>());
        Services.AddSingleton<ISecurityPolicyProvider>(
            new FixedSecurityPolicyProvider(SecurityPolicy.Default with { ApiKeysEnabled = true })
        );
        Services.AddSingleton(TimeProvider.System);
        Services.AddSingleton<ILogger<EntityPageBase>>(NullLogger<EntityPageBase>.Instance);
        ComponentFactories.AddStub<ResourceAccessPanel>();

        _mockUserContextAccessor
            .Setup(x => x.GetUserContextAsync())
            .ReturnsAsync(
                PageTestHelpers.CreateUserContext(
                    currentAccount: new Account { Id = Guid.CreateVersion7() }
                )
            );
        _mockRetrievalService
            .Setup(x => x.ListApiKeysAsync(It.IsAny<ListApiKeysRequest>()))
            .ReturnsAsync(
                new RetrieveListResult<ApiKey>(
                    RetrieveResultCode.Success,
                    string.Empty,
                    PagedResult<ApiKey>.Create([], 0, 0, 25)
                )
            );
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<Guid[]>())
            )
            .ReturnsAsync(true);
    }

    [Fact]
    public void ChildContent_RendersBetweenTheHeaderAndTheList_ForAPageThatPassesSome()
    {
        var cut = Render<ApiKeyList>(p =>
            p.AddChildContent("<section id=\"consumer-content\">Using the API</section>")
        );
        cut.WaitForAssertion(() => cut.Find("table"));

        var markup = cut.Markup;
        var header = markup.IndexOf("management-header", StringComparison.Ordinal);
        var content = markup.IndexOf("consumer-content", StringComparison.Ordinal);
        var table = markup.IndexOf("<table", StringComparison.Ordinal);
        Assert.InRange(content, header + 1, table - 1);
    }
}
