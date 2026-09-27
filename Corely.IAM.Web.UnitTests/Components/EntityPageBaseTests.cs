using Corely.IAM.Users.Models;
using Corely.IAM.Web.Components;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TestContext = Bunit.TestContext;

namespace Corely.IAM.Web.UnitTests.Components;

public class EntityPageBaseTests : TestContext
{
    private readonly Mock<IBlazorUserContextAccessor> _mockUserContextAccessor = new();

    public EntityPageBaseTests()
    {
        Services.AddSingleton(_mockUserContextAccessor.Object);
        Services.AddSingleton<ILogger<EntityPageBase>>(NullLogger<EntityPageBase>.Instance);
    }

    [Fact]
    public void BeforeTheUserContextResolves_IsLoading()
    {
        var contextGate = new TaskCompletionSource<UserContext?>();
        _mockUserContextAccessor.Setup(x => x.GetUserContextAsync()).Returns(contextGate.Task);

        var cut = Render<LoadingProbePage>();

        Assert.Equal("loading", cut.Markup);
        contextGate.SetResult(PageTestHelpers.CreateUserContext());
    }

    [Fact]
    public void AfterTheLoadCompletes_IsNotLoading()
    {
        _mockUserContextAccessor
            .Setup(x => x.GetUserContextAsync())
            .ReturnsAsync(PageTestHelpers.CreateUserContext());

        var cut = Render<LoadingProbePage>();

        cut.WaitForAssertion(() => Assert.Equal("loaded", cut.Markup));
    }

    private class LoadingProbePage : EntityPageBase
    {
        protected override Task LoadCoreAsync() => Task.CompletedTask;

        protected override void BuildRenderTree(RenderTreeBuilder builder) =>
            builder.AddContent(0, _loading ? "loading" : "loaded");
    }
}
