using Corely.IAM.Services;
using Corely.IAM.Web.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.Web.UnitTests.Extensions;

public class LinkGeneratorExtensionsTests
{
    private static async Task<string?> PathOnAHostThatAsync(
        Guid accountId,
        Action<WebApplication> map
    )
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["urls"] = "http://127.0.0.1:0";
        builder.Services.AddSingleton(Mock.Of<IRetrievalService>());
        await using var app = builder.Build();
        map(app);
        await app.StartAsync(Xunit.TestContext.Current.CancellationToken);
        var path = app.Services.GetRequiredService<LinkGenerator>().AccountJwksPath(accountId);
        await app.StopAsync(Xunit.TestContext.Current.CancellationToken);
        return path;
    }

    [Fact]
    public async Task AccountJwksPath_UsesTheHostsPrefix_WhenTheEndpointIsMapped()
    {
        var accountId = Guid.CreateVersion7();

        var path = await PathOnAHostThatAsync(accountId, app => app.MapIAMJwksEndpoints("/keys"));

        Assert.Equal($"/keys/{accountId}/.well-known/jwks.json", path);
    }

    [Fact]
    public async Task AccountJwksPath_IsNull_WhenTheHostHasNotMappedTheEndpoint()
    {
        var path = await PathOnAHostThatAsync(Guid.CreateVersion7(), _ => { });

        Assert.Null(path);
    }
}
