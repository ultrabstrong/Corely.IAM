using Corely.IAM.DataAccess;
using Corely.IAM.Models;
using Corely.IAM.PasswordRecoveries.Models;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.TotpAuths.Providers;
using Corely.IAM.Users.Entities;
using Corely.IAM.Web;
using Corely.IAM.Web.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.Web.FunctionalTests.Infrastructure;

public abstract class FunctionalTestBase : IAsyncLifetime
{
    protected IamWebApplicationFactory Factory { get; private set; } = null!;
    protected TestClient Client { get; private set; } = null!;
    protected TestTimeProvider Clock => Factory.TimeProvider;

    protected Guid OwnerUserId { get; private set; }
    protected Guid AccountId { get; private set; }

    protected virtual int AuthTokenTtlSeconds => 3600;
    protected virtual int AuthSessionTtlSeconds => 604800;

    public async Task InitializeAsync()
    {
        Factory = new IamWebApplicationFactory
        {
            AuthTokenTtlSeconds = AuthTokenTtlSeconds,
            AuthSessionTtlSeconds = AuthSessionTtlSeconds,
        };
        await Factory.InitializeDatabaseAsync();
        await SeedAsync();
        Client = new TestClient(Factory.CreateTestClient());
    }

    public Task DisposeAsync()
    {
        Client?.Dispose();
        Factory?.Dispose();
        return Task.CompletedTask;
    }

    private async Task SeedAsync()
    {
        await Factory.WithScopeAsync(async services =>
        {
            var owner = await services
                .GetRequiredService<IRegistrationService>()
                .RegisterUserAsync(
                    new RegisterUserRequest(
                        SeedData.OwnerUsername,
                        SeedData.OwnerEmail,
                        SeedData.OwnerPassword
                    )
                );
            Assert.Equal(RegisterUserResultCode.Success, owner.ResultCode);
            OwnerUserId = owner.CreatedUserId;
        });

        await Factory.WithScopeAsync(async services =>
        {
            var signIn = await services
                .GetRequiredService<IAuthenticationService>()
                .SignInAsync(
                    new SignInRequest(SeedData.OwnerUsername, SeedData.OwnerPassword, "seed-device")
                );
            Assert.Equal(SignInResultCode.Success, signIn.ResultCode);

            var account = await services
                .GetRequiredService<IRegistrationService>()
                .RegisterAccountAsync(
                    new RegisterAccountRequest(SeedData.AccountName, OwnerUserId)
                );
            Assert.Equal(RegisterAccountResultCode.Success, account.ResultCode);
            AccountId = account.CreatedAccountId;
        });
    }

    protected Task<HttpResponseMessage> SignInAsync(
        string username = SeedData.OwnerUsername,
        string password = SeedData.OwnerPassword
    ) =>
        Client.PostFormAsync(
            AppRoutes.SignIn,
            new Dictionary<string, string> { ["Username"] = username, ["Password"] = password }
        );

    protected async Task SignInSuccessfullyAsync()
    {
        using var response = await SignInAsync();
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(CurrentAuthToken);
    }

    protected async Task<Guid> CreateAdditionalAccountAsync(string accountName) =>
        await Factory.WithScopeAsync(async services =>
        {
            var signIn = await services
                .GetRequiredService<IAuthenticationService>()
                .SignInAsync(
                    new SignInRequest(SeedData.OwnerUsername, SeedData.OwnerPassword, "seed-device")
                );
            Assert.Equal(SignInResultCode.Success, signIn.ResultCode);

            var account = await services
                .GetRequiredService<IRegistrationService>()
                .RegisterAccountAsync(new RegisterAccountRequest(accountName, OwnerUserId));
            Assert.Equal(RegisterAccountResultCode.Success, account.ResultCode);
            return account.CreatedAccountId;
        });

    protected async Task<string> EnableMfaAsync() =>
        await Factory.WithScopeAsync(async services =>
        {
            var signIn = await services
                .GetRequiredService<IAuthenticationService>()
                .SignInAsync(
                    new SignInRequest(SeedData.OwnerUsername, SeedData.OwnerPassword, "seed-device")
                );
            Assert.Equal(SignInResultCode.Success, signIn.ResultCode);

            var mfa = services.GetRequiredService<IMfaService>();
            var enable = await mfa.EnableTotpAsync();
            Assert.Equal(EnableTotpResultCode.Success, enable.ResultCode);
            Assert.NotNull(enable.Secret);

            var code = services.GetRequiredService<ITotpProvider>().GenerateCode(enable.Secret!);
            var confirm = await mfa.ConfirmTotpAsync(new ConfirmTotpRequest(code));
            Assert.Equal(ConfirmTotpResultCode.Success, confirm.ResultCode);

            return enable.Secret!;
        });

    protected Task<string> GenerateTotpCodeAsync(string secret) =>
        Factory.WithScopeAsync(services =>
            Task.FromResult(services.GetRequiredService<ITotpProvider>().GenerateCode(secret))
        );

    protected Task<string?> RequestPasswordRecoveryTokenAsync(string email) =>
        Factory.WithScopeAsync(async services =>
        {
            var result = await services
                .GetRequiredService<IPasswordRecoveryService>()
                .RequestPasswordRecoveryAsync(new RequestPasswordRecoveryRequest(email));
            return result.RecoveryToken;
        });

    protected string? CurrentAuthToken => Client.Cookies[AuthenticationConstants.AUTH_TOKEN_COOKIE];

    protected string? CurrentAuthTokenId =>
        Client.Cookies[AuthenticationConstants.AUTH_TOKEN_ID_COOKIE];

    protected bool HasAuthCookies =>
        Client.Cookies.Contains(AuthenticationConstants.AUTH_TOKEN_COOKIE);

    internal Task<T> QueryAsync<T>(Func<IamDbContext, Task<T>> query) =>
        Factory.WithScopeAsync(services => query(services.GetRequiredService<IamDbContext>()));

    internal Task<UserAuthTokenEntity?> GetAuthTokenRowAsync(Guid id) =>
        QueryAsync(db => db.UserAuthTokens.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id));

    internal Task<List<UserAuthTokenEntity>> GetActiveAuthTokensAsync() =>
        QueryAsync(db =>
            db.UserAuthTokens.AsNoTracking()
                .Where(t => t.UserId == OwnerUserId && t.RevokedUtc == null)
                .ToListAsync()
        );

    protected async Task RevokeCurrentTokenOutOfBandAsync()
    {
        var tokenId = Guid.Parse(CurrentAuthTokenId!);
        var revokedAt = Clock.GetUtcNow().UtcDateTime;
        await Factory.WithScopeAsync(async services =>
        {
            var db = services.GetRequiredService<IamDbContext>();
            var row = await db.UserAuthTokens.FirstAsync(t => t.Id == tokenId);
            row.RevokedUtc = revokedAt;
            await db.SaveChangesAsync();
        });
    }
}
