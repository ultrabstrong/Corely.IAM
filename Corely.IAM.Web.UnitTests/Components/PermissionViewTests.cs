using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Providers;
using Corely.IAM.Web.Components.Shared;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using TestContext = Bunit.TestContext;

namespace Corely.IAM.Web.UnitTests.Components;

public class PermissionViewTests : TestContext
{
    private const string RESOURCE = "roles";
    private const string AUTHORIZED_MARKER = "authorized-content";
    private const string UNDETERMINED_MARKER = "undetermined-content";

    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly InlineOperationScopes _operationScopes;

    public PermissionViewTests()
    {
        _operationScopes = new InlineOperationScopes(Services);
        Services.AddSingleton<IOperationScopes>(_operationScopes);
        Services.AddSingleton(_mockAuthorizationProvider.Object);
        AuthorizeWith(Task.FromResult(true));
    }

    private void AuthorizeWith(Task<bool> answer) =>
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<Guid[]>())
            )
            .Returns(answer);

    private IRenderedComponent<PermissionView> RenderView(Guid[]? resourceIds = null) =>
        Render<PermissionView>(parameters =>
            parameters
                .Add(p => p.Action, AuthAction.Create)
                .Add(p => p.Resource, RESOURCE)
                .Add(p => p.ResourceIds, resourceIds)
                .Add(
                    p => p.ChildContent,
                    (RenderFragment)(
                        b => b.AddMarkupContent(0, $"<div id=\"{AUTHORIZED_MARKER}\"></div>")
                    )
                )
                .Add(p => p.NotAuthorized, "<div id=\"denied\"></div>")
                .Add(p => p.Undetermined, $"<div id=\"{UNDETERMINED_MARKER}\"></div>")
        );

    [Fact]
    public void PermissionView_RendersAuthorized_WhenTheCheckPasses()
    {
        var cut = RenderView();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll($"#{AUTHORIZED_MARKER}")));
    }

    [Fact]
    public void PermissionView_RendersNotAuthorized_WhenTheUserGenuinelyLacksThePermission()
    {
        AuthorizeWith(Task.FromResult(false));

        var cut = RenderView();

        Assert.Single(cut.FindAll("#denied"));
        Assert.Empty(cut.FindAll($"#{AUTHORIZED_MARKER}"));
    }

    [Fact]
    public void PermissionView_RendersUndetermined_WhileTheCheckIsRunning()
    {
        var answer = new TaskCompletionSource<bool>();
        AuthorizeWith(answer.Task);

        var cut = RenderView();

        Assert.Single(cut.FindAll($"#{UNDETERMINED_MARKER}"));
        Assert.Empty(cut.FindAll($"#{AUTHORIZED_MARKER}"));
        answer.SetResult(true);
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll($"#{AUTHORIZED_MARKER}"));
            Assert.Empty(cut.FindAll($"#{UNDETERMINED_MARKER}"));
        });
    }

    [Fact]
    public void PermissionView_RendersNeitherAnswer_ForASessionThatEnded()
    {
        _operationScopes.SignedIn = false;

        var cut = RenderView();

        Assert.Empty(cut.FindAll($"#{AUTHORIZED_MARKER}"));
        Assert.Empty(cut.FindAll("#denied"));
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<Guid[]>()),
            Times.Never
        );
    }

    [Fact]
    public void PermissionView_RendersUndetermined_ForAnAnonymousVisitor()
    {
        _operationScopes.HasSession = false;

        var cut = RenderView();

        Assert.Single(cut.FindAll($"#{UNDETERMINED_MARKER}"));
        Assert.Empty(cut.FindAll("#denied"));
    }

    [Fact]
    public void PermissionView_OpensNoOperation_ForAParentRenderWithTheSameQuestion()
    {
        var id = Guid.NewGuid();
        var cut = RenderView([id]);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll($"#{AUTHORIZED_MARKER}")));

        cut.Render(parameters => parameters.Add(p => p.ResourceIds, new[] { id }));

        Assert.Equal(1, _operationScopes.Runs);
    }

    [Fact]
    public void PermissionView_ChecksAgain_ForADifferentResource()
    {
        var cut = RenderView([Guid.NewGuid()]);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll($"#{AUTHORIZED_MARKER}")));

        cut.Render(parameters => parameters.Add(p => p.ResourceIds, new[] { Guid.NewGuid() }));

        Assert.Equal(2, _operationScopes.Runs);
    }
}
