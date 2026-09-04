using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Components.Shared;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using TestContext = Bunit.TestContext;

namespace Corely.IAM.Web.UnitTests.Components;

public class PermissionViewTests : TestContext
{
    private const string RESOURCE = "roles";
    private const string AUTHORIZED_MARKER = "authorized-content";
    private const string UNDETERMINED_MARKER = "undetermined-content";

    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly Mock<IUserContextProvider> _mockUserContextProvider = new();

    private UserContext? _userContext;

    public PermissionViewTests()
    {
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns(() => _userContext);

        // Mirrors AuthorizationProvider: with no user context there is nothing to authorize
        // against, so every check is denied.
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<Guid[]>())
            )
            .ReturnsAsync(() => _userContext != null);

        Services.AddSingleton(_mockAuthorizationProvider.Object);
        Services.AddSingleton(_mockUserContextProvider.Object);
    }

    /// <summary>
    /// Stands in for AuthenticatedPageBase: its OnInitializedAsync yields before the user context
    /// exists, which makes Blazor paint an interim render of the children first.
    /// </summary>
    private sealed class DeferredContextPage : ComponentBase
    {
        [Parameter]
        public TaskCompletionSource Gate { get; set; } = null!;

        [Parameter]
        public Action OnContextResolved { get; set; } = null!;

        [Parameter]
        public bool ShowUndetermined { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await Gate.Task;
            OnContextResolved();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<PermissionView>(0);
            builder.AddComponentParameter(1, nameof(PermissionView.Action), AuthAction.Create);
            builder.AddComponentParameter(2, nameof(PermissionView.Resource), RESOURCE);
            builder.AddComponentParameter(
                3,
                nameof(PermissionView.ChildContent),
                (RenderFragment)(
                    b => b.AddMarkupContent(0, $"<div id=\"{AUTHORIZED_MARKER}\"></div>")
                )
            );
            if (ShowUndetermined)
            {
                builder.AddComponentParameter(
                    4,
                    nameof(PermissionView.Undetermined),
                    (RenderFragment)(
                        b => b.AddMarkupContent(0, $"<div id=\"{UNDETERMINED_MARKER}\"></div>")
                    )
                );
            }
            builder.CloseComponent();
        }
    }

    private IRenderedComponent<DeferredContextPage> RenderDeferredPage(
        TaskCompletionSource gate,
        bool showUndetermined = false
    ) =>
        Render<DeferredContextPage>(parameters =>
            parameters
                .Add(p => p.Gate, gate)
                .Add(p => p.ShowUndetermined, showUndetermined)
                .Add(
                    p => p.OnContextResolved,
                    () => _userContext = PageTestHelpers.CreateUserContext()
                )
        );

    [Fact]
    public void PermissionView_RendersAuthorized_WhenUserContextArrivesAfterTheFirstCheck()
    {
        var gate = new TaskCompletionSource();
        var cut = RenderDeferredPage(gate);

        gate.SetResult();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll($"#{AUTHORIZED_MARKER}")));
    }

    [Fact]
    public void PermissionView_RendersNothing_WhileTheUserContextIsStillUnknown()
    {
        var gate = new TaskCompletionSource();

        var cut = RenderDeferredPage(gate);

        // Not yet known is not the same as denied - nothing should be committed either way.
        Assert.Empty(cut.FindAll($"#{AUTHORIZED_MARKER}"));
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<Guid[]>()),
            Times.Never
        );
    }

    [Fact]
    public void PermissionView_RendersNotAuthorized_WhenTheUserGenuinelyLacksThePermission()
    {
        _userContext = PageTestHelpers.CreateUserContext();
        _mockAuthorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<Guid[]>())
            )
            .ReturnsAsync(false);

        var cut = Render<PermissionView>(parameters =>
            parameters
                .Add(p => p.Action, AuthAction.Create)
                .Add(p => p.Resource, RESOURCE)
                .Add(p => p.NotAuthorized, "<div id=\"denied\"></div>")
        );

        Assert.Single(cut.FindAll("#denied"));
    }

    [Fact]
    public void PermissionView_ChecksOnce_WhenTheContextIsAlreadyAvailable()
    {
        _userContext = PageTestHelpers.CreateUserContext();
        var gate = new TaskCompletionSource();
        gate.SetResult();

        var cut = RenderDeferredPage(gate);

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll($"#{AUTHORIZED_MARKER}")));
        _mockAuthorizationProvider.Verify(
            x =>
                x.IsAuthorizedAsync(It.IsAny<AuthAction>(), It.IsAny<string>(), It.IsAny<Guid[]>()),
            Times.Once
        );
    }

    [Fact]
    public void PermissionView_RendersUndetermined_WhileTheUserContextIsStillUnknown()
    {
        var gate = new TaskCompletionSource();

        var cut = RenderDeferredPage(gate, showUndetermined: true);

        Assert.Single(cut.FindAll($"#{UNDETERMINED_MARKER}"));
        Assert.Empty(cut.FindAll($"#{AUTHORIZED_MARKER}"));
    }

    [Fact]
    public void PermissionView_ReplacesUndetermined_OnceTheContextResolves()
    {
        var gate = new TaskCompletionSource();
        var cut = RenderDeferredPage(gate, showUndetermined: true);

        gate.SetResult();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll($"#{AUTHORIZED_MARKER}"));
            Assert.Empty(cut.FindAll($"#{UNDETERMINED_MARKER}"));
        });
    }
}
