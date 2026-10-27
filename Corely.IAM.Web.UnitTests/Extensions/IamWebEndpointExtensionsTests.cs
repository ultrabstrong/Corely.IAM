using Corely.IAM.Web.Extensions;
using Microsoft.AspNetCore.Builder;

namespace Corely.IAM.Web.UnitTests.Extensions;

public class IamWebEndpointExtensionsTests
{
    [Fact]
    public void MapIAMTokenEndpoints_RefusesToMap_WithoutBearerTokens_AndSaysWhatToAdd()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddIAMWeb();
        var app = builder.Build();

        var error = Assert.Throws<InvalidOperationException>(() => app.MapIAMTokenEndpoints());

        Assert.Contains("options.AcceptBearerTokens = true", error.Message);
        Assert.Contains("AddIAMWeb", error.Message);
    }

    [Fact]
    public void MapIAMTokenEndpoints_Maps_WithBearerTokens()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddIAMWeb(options => options.AcceptBearerTokens = true);
        var app = builder.Build();

        var group = app.MapIAMTokenEndpoints("/api/auth");

        Assert.NotNull(group);
    }
}
