using Bunit.TestDoubles;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Web.Components;
using Corely.IAM.Web.Components.Pages.Users;
using Corely.IAM.Web.Components.Shared;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Corely.IAM.Web.UnitTests.Pages.Users;

public class UserDetailTests : TestContext
{
    private readonly Mock<IBlazorUserContextAccessor> _mockUserContextAccessor = new();
    private readonly Mock<IRetrievalService> _mockRetrievalService = new();
    private readonly Mock<IRegistrationService> _mockRegistrationService = new();
    private readonly Mock<IDeregistrationService> _mockDeregistrationService = new();

    public UserDetailTests()
    {
        Services.AddSingleton(_mockUserContextAccessor.Object);
        Services.AddSingleton(_mockRetrievalService.Object);
        Services.AddSingleton(_mockRegistrationService.Object);
        Services.AddSingleton(_mockDeregistrationService.Object);
        Services.AddSingleton<ILogger<EntityPageBase>>(NullLogger<EntityPageBase>.Instance);

        ComponentFactories.AddStub<PermissionView>();
        ComponentFactories.AddStub<EffectivePermissionsPanel>();
        ComponentFactories.AddStub<Pagination>();
    }

    [Fact]
    public void UserDetail_RendersEncryptionSigningPanel_WhenViewingOwnUser()
    {
        var userId = Guid.CreateVersion7();
        var userContext = PageTestHelpers.CreateUserContext(
            user: new User
            {
                Id = userId,
                Username = "self",
                Email = "self@test.com",
            }
        );
        var resultUser = new User
        {
            Id = userId,
            Username = "self",
            Email = "self@test.com",
        };

        _mockUserContextAccessor.Setup(x => x.GetUserContextAsync()).ReturnsAsync(userContext);
        _mockRetrievalService
            .Setup(x => x.GetUserAsync(It.IsAny<Guid>(), It.IsAny<bool>()))
            .ReturnsAsync(
                new RetrieveSingleResult<User>(
                    RetrieveResultCode.Success,
                    string.Empty,
                    resultUser,
                    null
                )
            );
        _mockRetrievalService
            .Setup(x => x.GetUserSymmetricEncryptionProviderAsync())
            .ReturnsAsync(
                new RetrieveSingleResult<IIamSymmetricEncryptionProvider>(
                    RetrieveResultCode.Success,
                    string.Empty,
                    null,
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

        var cut = RenderComponent<UserDetail>(parameters => parameters.Add(p => p.Id, userId));

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindComponents<EncryptionSigningPanel>());
            _mockRetrievalService.Verify(x => x.GetUserAsync(userId, true), Times.Once);
            _mockRetrievalService.Verify(
                x => x.GetUserSymmetricEncryptionProviderAsync(),
                Times.Once
            );
            _mockRetrievalService.Verify(
                x => x.GetUserAsymmetricEncryptionProviderAsync(),
                Times.Once
            );
            _mockRetrievalService.Verify(
                x => x.GetUserAsymmetricSignatureProviderAsync(),
                Times.Once
            );
        });
    }

    [Fact]
    public void UserDetail_HidesEncryptionSigningPanel_WhenViewingDifferentUser()
    {
        var currentUserId = Guid.CreateVersion7();
        var targetUserId = Guid.CreateVersion7();
        var userContext = PageTestHelpers.CreateUserContext(
            user: new User
            {
                Id = currentUserId,
                Username = "self",
                Email = "self@test.com",
            }
        );
        var resultUser = new User
        {
            Id = targetUserId,
            Username = "other",
            Email = "other@test.com",
        };

        _mockUserContextAccessor.Setup(x => x.GetUserContextAsync()).ReturnsAsync(userContext);
        _mockRetrievalService
            .Setup(x => x.GetUserAsync(It.IsAny<Guid>(), It.IsAny<bool>()))
            .ReturnsAsync(
                new RetrieveSingleResult<User>(
                    RetrieveResultCode.Success,
                    string.Empty,
                    resultUser,
                    null
                )
            );

        var cut = RenderComponent<UserDetail>(parameters =>
            parameters.Add(p => p.Id, targetUserId)
        );

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindComponents<EncryptionSigningPanel>());
            _mockRetrievalService.Verify(x => x.GetUserAsync(targetUserId, true), Times.Once);
            _mockRetrievalService.Verify(
                x => x.GetUserSymmetricEncryptionProviderAsync(),
                Times.Never
            );
            _mockRetrievalService.Verify(
                x => x.GetUserAsymmetricEncryptionProviderAsync(),
                Times.Never
            );
            _mockRetrievalService.Verify(
                x => x.GetUserAsymmetricSignatureProviderAsync(),
                Times.Never
            );
        });
    }
}
