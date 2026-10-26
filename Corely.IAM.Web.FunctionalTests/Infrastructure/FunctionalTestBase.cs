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
using Microsoft.Extensions.Hosting;

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
    protected virtual int AuthSessionIdleTimeoutSeconds => 86400;
    protected virtual string EnvironmentName => Environments.Development;
    protected virtual IReadOnlyDictionary<string, string> Settings =>
        new Dictionary<string, string>();

    protected virtual void ConfigureTestServices(IServiceCollection services) { }

    public async ValueTask InitializeAsync()
    {
        Factory = new IamWebApplicationFactory
        {
            AuthTokenTtlSeconds = AuthTokenTtlSeconds,
            AuthSessionTtlSeconds = AuthSessionTtlSeconds,
            AuthSessionIdleTimeoutSeconds = AuthSessionIdleTimeoutSeconds,
            EnvironmentName = EnvironmentName,
            Settings = Settings,
            TestServices = ConfigureTestServices,
        };
        await Factory.InitializeDatabaseAsync();
        await SeedAsync();
        Client = new TestClient(Factory.CreateTestClient());
    }

    public ValueTask DisposeAsync()
    {
        Client?.Dispose();
        Factory?.Dispose();
        return ValueTask.CompletedTask;
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

    protected async Task<string> GetVerifyMfaPageAsync()
    {
        await EnableMfaAsync();
        using (var signIn = await SignInAsync())
            Assert.Equal(System.Net.HttpStatusCode.Redirect, signIn.StatusCode);

        using var page = await Client.GetAsync(AppRoutes.VerifyMfa);
        Assert.Equal(System.Net.HttpStatusCode.OK, page.StatusCode);
        return await page.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

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

    protected string? CurrentAuthToken => Client.Cookies[AuthCookieManager.AUTH_TOKEN_COOKIE];

    protected string? CurrentAuthTokenId => Client.Cookies[AuthCookieManager.AUTH_TOKEN_ID_COOKIE];

    protected string? CurrentRefreshToken => Client.Cookies[AuthCookieManager.REFRESH_TOKEN_COOKIE];

    protected TestClient CreateClientWith(IReadOnlyDictionary<string, string> cookies)
    {
        var client = new TestClient(Factory.CreateTestClient());
        client.Cookies.Restore(cookies);
        return client;
    }

    internal async Task<UserAuthTokenEntity> CurrentAuthTokenRowAsync() =>
        (await GetAuthTokenRowAsync(Guid.Parse(CurrentAuthTokenId!)))!;

    protected bool HasAuthCookies => Client.Cookies.Contains(AuthCookieManager.AUTH_TOKEN_COOKIE);

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
