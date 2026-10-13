using Corely.IAM.Web.Components.Shared;
using TestContext = Bunit.TestContext;

namespace Corely.IAM.Web.UnitTests.Components.Shared;

public class CopyableIdTests : TestContext
{
    private readonly Guid _id = Guid.CreateVersion7();

    [Fact]
    public void ShowsTheId_WithACopyButton()
    {
        var cut = Render<CopyableId>(p => p.Add(x => x.Value, _id));

        Assert.Equal(_id.ToString(), cut.Find(".guid-text").TextContent);
        Assert.Equal($"Copy {_id}", cut.Find("button.copy-btn").GetAttribute("aria-label"));
    }

    [Fact]
    public void Copy_WritesTheIdToTheClipboard_AndShowsItWasCopied()
    {
        var copy = JSInterop.SetupVoid("navigator.clipboard.writeText", _id.ToString());
        copy.SetVoidResult();
        var cut = Render<CopyableId>(p => p.Add(x => x.Value, _id));

        cut.Find("button.copy-btn").Click();

        copy.VerifyInvoke("navigator.clipboard.writeText");
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("button.copy-btn .bi-check2")));
    }
}
