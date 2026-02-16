using System.Security.Claims;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Users.Models;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;

namespace Corely.IAM.Web.UnitTests.Services;

public class IamAuthenticationStateProviderTests
{
    private readonly Mock<IBlazorUserContextAccessor> _mockAccessor = new();
    private readonly IamAuthenticationStateProvider _provider;

    public IamAuthenticationStateProviderTests()
    {
        _provider = new IamAuthenticationStateProvider(
            _mockAccessor.Object,
            new UserContextClaimsBuilder()
        );
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_AuthenticatedUser_ReturnsAuthenticatedState()
    {
        var userId = Guid.CreateVersion7();
        var accountId = Guid.CreateVersion7();
        var user = new User
        {
            Id = userId,
            Username = "jdoe",
            Email = "jdoe@example.com",
        };
        var account = new Account { Id = accountId, AccountName = "Acme Corp" };
        var userContext = new UserContext(user, account, "test-device", [account]);

        _mockAccessor.Setup(a => a.GetUserContextAsync()).ReturnsAsync(userContext);

        var state = await _provider.GetAuthenticationStateAsync();

        Assert.NotNull(state.User.Identity);
        Assert.True(state.User.Identity.IsAuthenticated);
        Assert.Equal(userId.ToString(), state.User.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("jdoe", state.User.FindFirstValue(ClaimTypes.Name));
        Assert.Equal("jdoe@example.com", state.User.FindFirstValue(ClaimTypes.Email));
        Assert.Equal(
            accountId.ToString(),
            state.User.FindFirstValue(AuthenticationConstants.ACCOUNT_ID_CLAIM)
        );
        Assert.Equal(
            "Acme Corp",
            state.User.FindFirstValue(AuthenticationConstants.ACCOUNT_NAME_CLAIM)
        );
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_NullUserContext_ReturnsUnauthenticatedState()
    {
        _mockAccessor.Setup(a => a.GetUserContextAsync()).ReturnsAsync((UserContext?)null);

        var state = await _provider.GetAuthenticationStateAsync();

        Assert.NotNull(state.User.Identity);
        Assert.False(state.User.Identity.IsAuthenticated);
    }
}
