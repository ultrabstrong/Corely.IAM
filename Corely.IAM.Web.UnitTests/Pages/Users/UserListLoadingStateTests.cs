using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
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
using TestContext = Bunit.TestContext;

namespace Corely.IAM.Web.UnitTests.Pages.Users;

/// <summary>
/// A list must not claim to be empty before it knows. This is the same defect as the one
/// PermissionView had - an unknown state rendered as a known one - and it reached users twice, so
/// it is worth a test rather than a careful reading.
/// </summary>
public class UserListLoadingStateTests : TestContext
{
    private const string EMPTY_STATE_TEXT = "No users found";

    private readonly Mock<IBlazorUserContextAccessor> _mockUserContextAccessor = new();
    private readonly Mock<IRetrievalService> _mockRetrievalService = new();
    private readonly Mock<IDeregistrationService> _mockDeregistrationService = new();

    public UserListLoadingStateTests()
    {
        _mockUserContextAccessor
            .Setup(x => x.GetUserContextAsync())
            .ReturnsAsync(
                // The page reads UserContext.CurrentAccount.Id, so a context without one throws
                // before the service is ever called.
                PageTestHelpers.CreateUserContext(
                    currentAccount: new Account { Id = Guid.CreateVersion7(), AccountName = "Test" }
                )
            );

        Services.AddSingleton(_mockUserContextAccessor.Object);
        Services.AddSingleton(_mockRetrievalService.Object);
        Services.AddSingleton(_mockDeregistrationService.Object);
        Services.AddSingleton<ILogger<Corely.IAM.Web.Components.EntityPageBase>>(
            NullLogger<Corely.IAM.Web.Components.EntityPageBase>.Instance
        );

        ComponentFactories.AddStub<PermissionView>();
        ComponentFactories.AddStub<Pagination>();
    }

    [Fact]
    public void BeforeTheUserContextResolves_ShowsNothingRatherThanAnEmptyState()
    {
        // The page awaits its user context before it can even ask for data. That await yields, so
        // Blazor paints an interim render first - which is the frame the empty state used to leak
        // into. A synchronously-completing mock never produces that frame, so the gate has to be
        // held open deliberately.
        var contextGate = new TaskCompletionSource<UserContext?>();
        _mockUserContextAccessor.Setup(x => x.GetUserContextAsync()).Returns(contextGate.Task);

        var cut = Render<UserList>();

        Assert.DoesNotContain(EMPTY_STATE_TEXT, cut.Markup);

        contextGate.SetResult(
            PageTestHelpers.CreateUserContext(
                currentAccount: new Account { Id = Guid.CreateVersion7(), AccountName = "Test" }
            )
        );
    }

    [Fact]
    public void WhileTheResultIsUnknown_ShowsLoadingRatherThanAnEmptyState()
    {
        // Never completes, so the component stays in the state it is in before an answer arrives.
        _mockRetrievalService
            .Setup(x => x.ListUsersAsync(It.IsAny<ListUsersRequest>()))
            .Returns(new TaskCompletionSource<RetrieveListResult<User>>().Task);

        var cut = Render<UserList>();

        Assert.DoesNotContain(EMPTY_STATE_TEXT, cut.Markup);
        Assert.NotEmpty(cut.FindComponents<LoadingSpinner>());
    }

    [Fact]
    public void WhenTheResultIsGenuinelyEmpty_ShowsTheEmptyState()
    {
        _mockRetrievalService
            .Setup(x => x.ListUsersAsync(It.IsAny<ListUsersRequest>()))
            .ReturnsAsync(
                new RetrieveListResult<User>(
                    RetrieveResultCode.Success,
                    string.Empty,
                    PagedResult<User>.Create([], 0, 0, 25)
                )
            );

        var cut = Render<UserList>();

        cut.WaitForAssertion(() => Assert.Contains(EMPTY_STATE_TEXT, cut.Markup));
    }

    [Fact]
    public void WhenTheLoadFails_StopsLoadingRatherThanSpinningForever()
    {
        _mockRetrievalService
            .Setup(x => x.ListUsersAsync(It.IsAny<ListUsersRequest>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var cut = Render<UserList>();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindComponents<LoadingSpinner>()));
    }
}
