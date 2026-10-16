using Corely.IAM.Web.Extensions;
using Corely.IAM.Web.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.Web.UnitTests.Security;

public class HumanVerifierTests
{
    [Fact]
    public async Task NoHumanVerifier_AlwaysSucceeds()
    {
        var result = await new NoHumanVerifier().VerifyAsync(
            new DefaultHttpContext(),
            "register",
            CancellationToken.None
        );

        Assert.Equal(VerifyHumanResultCode.Success, result.ResultCode);
    }

    [Theory]
    [InlineData(VerifyHumanResultCode.Success, null)]
    [InlineData(VerifyHumanResultCode.MissingToken, VerifyHumanResult.COMPLETE_THE_CHECK)]
    [InlineData(VerifyHumanResultCode.Rejected, VerifyHumanResult.COMPLETE_THE_CHECK)]
    [InlineData(VerifyHumanResultCode.Unavailable, VerifyHumanResult.CHECK_UNAVAILABLE)]
    public void RefusalMessage_IsTheSameWordingForEveryPage(
        VerifyHumanResultCode code,
        string? expected
    )
    {
        Assert.Equal(expected, new VerifyHumanResult(code, "detail").RefusalMessage);
    }

    [Fact]
    public void AddIAMWeb_RegistersNoHumanVerifier_WhenTheHostRegisteredNone()
    {
        var services = new ServiceCollection();

        services.AddIAMWeb();

        Assert.IsType<NoHumanVerifier>(Resolve(services));
    }

    [Fact]
    public void AddIAMWeb_KeepsTheHostsVerifier_RegisteredBefore()
    {
        var services = new ServiceCollection();
        services.AddScoped<IHumanVerifier, HostVerifier>();

        services.AddIAMWeb();

        Assert.IsType<HostVerifier>(Resolve(services));
    }

    [Fact]
    public void AddIAMWeb_YieldsToTheHostsVerifier_RegisteredAfter()
    {
        var services = new ServiceCollection();
        services.AddIAMWeb();

        services.AddScoped<IHumanVerifier, HostVerifier>();

        Assert.IsType<HostVerifier>(Resolve(services));
    }

    private static IHumanVerifier Resolve(IServiceCollection services) =>
        services
            .BuildServiceProvider()
            .CreateScope()
            .ServiceProvider.GetRequiredService<IHumanVerifier>();

    private sealed class HostVerifier : IHumanVerifier
    {
        public Task<VerifyHumanResult> VerifyAsync(
            HttpContext httpContext,
            string action,
            CancellationToken cancellationToken
        ) => Task.FromResult(new VerifyHumanResult(VerifyHumanResultCode.Rejected, "host"));
    }
}
