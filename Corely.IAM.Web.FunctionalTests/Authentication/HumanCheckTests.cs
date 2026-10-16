using System.Net;
using Corely.IAM.DataAccess;
using Corely.IAM.Web.FunctionalTests.Infrastructure;
using Corely.IAM.Web.Security;
using Corely.IAM.WebApp;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

public class HumanCheckTests : FunctionalTestBase
{
    private const string NewUsername = "bot-user";

    protected override void ConfigureTestServices(IServiceCollection services) =>
        services.AddScoped<IHumanVerifier, RefusingVerifier>();

    [Fact]
    public async Task SignUp_RefusedByTheHumanCheck_CreatesNoUser()
    {
        using var response = await Client.PostFormAsync(
            AppRoutes.Register,
            new Dictionary<string, string>
            {
                ["Username"] = NewUsername,
                ["Email"] = "bot@example.com",
                ["Password"] = "Bot!Pass1234",
                ["ConfirmPassword"] = "Bot!Pass1234",
            }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(
            VerifyHumanResult.COMPLETE_THE_CHECK,
            await response.Content.ReadAsStringAsync()
        );
        Assert.False(
            await Factory.WithScopeAsync(services =>
                services
                    .GetRequiredService<IamDbContext>()
                    .Users.AnyAsync(u => u.Username == NewUsername)
            )
        );
    }

    [Fact]
    public async Task ForgotPassword_RefusedByTheHumanCheck_IssuesNoRecovery()
    {
        using var response = await Client.PostFormAsync(
            WebAppRoutes.ForgotPassword,
            new Dictionary<string, string> { ["Email"] = SeedData.OwnerEmail }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(
            VerifyHumanResult.COMPLETE_THE_CHECK,
            await response.Content.ReadAsStringAsync()
        );
    }

    private sealed class RefusingVerifier : IHumanVerifier
    {
        public Task<VerifyHumanResult> VerifyAsync(
            HttpContext httpContext,
            string action,
            CancellationToken cancellationToken
        ) => Task.FromResult(new VerifyHumanResult(VerifyHumanResultCode.Rejected, "bot"));
    }
}
