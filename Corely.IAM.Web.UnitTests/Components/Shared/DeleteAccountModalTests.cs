using Corely.IAM.Web.Components.Shared;
using Corely.IAM.Web.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TestContext = Bunit.TestContext;

namespace Corely.IAM.Web.UnitTests.Components.Shared;

public class DeleteAccountModalTests : TestContext
{
    private const string ACCOUNT_NAME = "Acme Co";

    private readonly IAMWebOptions _options = new();
    private int _confirmed;

    public DeleteAccountModalTests()
    {
        Services.AddSingleton(Options.Create(_options));
    }

    private IRenderedComponent<DeleteAccountModal> RenderShown()
    {
        var cut = Render<DeleteAccountModal>(p =>
            p.Add(m => m.AccountName, ACCOUNT_NAME)
                .Add(m => m.OnConfirm, EventCallback.Factory.Create(this, () => _confirmed++))
        );
        cut.InvokeAsync(() => cut.Instance.Show());
        return cut;
    }

    private static AngleSharp.Dom.IElement DeleteButton(
        IRenderedComponent<DeleteAccountModal> cut
    ) => cut.Find("button.btn-danger");

    [Fact]
    public void Delete_StaysDisabled_UntilTheNameIsTyped()
    {
        var cut = RenderShown();

        Assert.True(DeleteButton(cut).HasAttribute("disabled"));
    }

    [Theory]
    [InlineData("Acme")]
    [InlineData("acme co")]
    [InlineData("Acme Co ")]
    public void Delete_StaysDisabled_ForANameThatDoesNotMatchExactly(string typed)
    {
        var cut = RenderShown();

        cut.Find("#delete-account-confirmation").Input(typed);

        Assert.True(DeleteButton(cut).HasAttribute("disabled"));
    }

    [Fact]
    public void Delete_ConfirmsOnce_ForTheExactName()
    {
        var cut = RenderShown();

        cut.Find("#delete-account-confirmation").Input(ACCOUNT_NAME);
        Assert.False(DeleteButton(cut).HasAttribute("disabled"));
        DeleteButton(cut).Click();

        Assert.Equal(1, _confirmed);
    }

    [Fact]
    public void Show_ClearsWhatWasTyped_ForEveryOpening()
    {
        var cut = RenderShown();
        cut.Find("#delete-account-confirmation").Input(ACCOUNT_NAME);
        cut.Find("button.btn-secondary").Click();

        cut.InvokeAsync(() => cut.Instance.Show());

        Assert.True(DeleteButton(cut).HasAttribute("disabled"));
    }

    [Fact]
    public void Show_RendersTheHostsNotice_WhenOneIsSet()
    {
        _options.AccountDeletionNotice = typeof(TestNotice);

        var cut = RenderShown();

        Assert.NotNull(cut.Find(".test-notice"));
        Assert.DoesNotContain("This cannot be undone", cut.Markup);
    }

    private sealed class TestNotice : ComponentBase
    {
        protected override void BuildRenderTree(
            Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder
        )
        {
            builder.OpenElement(0, "p");
            builder.AddAttribute(1, "class", "test-notice");
            builder.AddContent(2, "Everything goes.");
            builder.CloseElement();
        }
    }
}
