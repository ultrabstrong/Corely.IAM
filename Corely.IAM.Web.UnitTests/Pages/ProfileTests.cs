using Bunit.TestDoubles;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Web.Components;
using Corely.IAM.Web.Components.Pages;
using Corely.IAM.Web.Components.Shared;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TestContext = Bunit.TestContext;

namespace Corely.IAM.Web.UnitTests.Pages;

public class ProfileTests : TestContext
{
    private readonly Mock<IBlazorUserContextAccessor> _mockUserContextAccessor = new();
    private readonly Mock<IRetrievalService> _mockRetrievalService = new();
    private readonly Mock<IModificationService> _mockModificationService = new();

    public ProfileTests()
    {
        Services.AddSingleton(_mockUserContextAccessor.Object);
        Services.AddSingleton(_mockRetrievalService.Object);
        Services.AddSingleton(_mockModificationService.Object);
        Services.AddSingleton(new Mock<IDeregistrationService>().Object);
        var googleAuth = new Mock<IGoogleAuthService>();
        googleAuth
            .Setup(x => x.GetAuthMethodsAsync())
            .ReturnsAsync(
                new AuthMethodsResult(
                    AuthMethodsResultCode.Success,
                    string.Empty,
                    true,
                    false,
                    null
                )
            );
        Services.AddSingleton(googleAuth.Object);
        Services.AddSingleton<ILogger<EntityPageBase>>(NullLogger<EntityPageBase>.Instance);
        JSInterop.Mode = JSRuntimeMode.Loose;

        ComponentFactories.AddStub<Pagination>();
        ComponentFactories.AddStub<TotpSection>();
        ComponentFactories.AddStub<PasswordSection>();
        ComponentFactories.AddStub<LinkedAccountsSection>();
    }

    private static IIamSymmetricEncryptionProvider SymmetricProvider(string description)
    {
        var provider = new Mock<IIamSymmetricEncryptionProvider>();
        provider.Setup(x => x.ProviderName).Returns("AES");
        provider.Setup(x => x.ProviderDescription).Returns(description);
        return provider.Object;
    }

    [Fact]
    public void Profile_ShowsTheReloadedKey_AfterRotating()
    {
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Username = "self",
            Email = "self@test.com",
        };
        _mockUserContextAccessor
            .Setup(x => x.GetUserContextAsync())
            .ReturnsAsync(PageTestHelpers.CreateUserContext(user: user));
        _mockRetrievalService
            .Setup(x => x.GetUserAsync(user.Id, false))
            .ReturnsAsync(
                new RetrieveSingleResult<User>(RetrieveResultCode.Success, string.Empty, user, null)
            );
        _mockRetrievalService
            .SetupSequence(x => x.GetUserSymmetricEncryptionProviderAsync())
            .ReturnsAsync(
                new RetrieveSingleResult<IIamSymmetricEncryptionProvider>(
                    RetrieveResultCode.Success,
                    string.Empty,
                    SymmetricProvider("version one"),
                    null
                )
            )
            .ReturnsAsync(
                new RetrieveSingleResult<IIamSymmetricEncryptionProvider>(
                    RetrieveResultCode.Success,
                    string.Empty,
                    SymmetricProvider("version two"),
                    null
                )
            );
        _mockRetrievalService
            .Setup(x => x.GetUserAsymmetricEncryptionProviderAsync())
            .ReturnsAsync(
                new RetrieveSingleResult<IIamAsymmetricEncryptionProvider>(
                    RetrieveResultCode.Success,
                    string.Empty,
                    null,
                    null
                )
            );
        _mockRetrievalService
            .Setup(x => x.GetUserAsymmetricSignatureProviderAsync())
            .ReturnsAsync(
                new RetrieveSingleResult<IIamAsymmetricSignatureProvider>(
                    RetrieveResultCode.Success,
                    string.Empty,
                    null,
                    null
                )
            );
        _mockModificationService
            .Setup(x => x.RotateCurrentUserKeyAsync(KeyType.SymmetricEncryption))
            .ReturnsAsync(new ModifyResult(ModifyResultCode.Success, string.Empty));

        var cut = Render<Profile>();
        cut.WaitForAssertion(() => Assert.Contains("version one", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent == "Rotate key").Click();
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Rotate").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("version two", cut.Markup);
            Assert.DoesNotContain("version one", cut.Markup);
        });
    }
}
