using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.FederatedIdentities.Entities;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.Platform.Providers;
using Corely.IAM.Validators;
using Corely.Security.Hashing.Factories;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.BasicAuths.Processors;

public class BasicAuthProcessorDeleteTests
{
    private const string VALID_PASSWORD = "Test1234";

    private readonly ServiceFactory _serviceFactory = new();
    private readonly BasicAuthProcessor _basicAuthProcessor;

    public BasicAuthProcessorDeleteTests()
    {
        _basicAuthProcessor = new BasicAuthProcessor(
            _serviceFactory.GetRequiredService<IRepo<BasicAuthEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<FederatedIdentityEntity>>(),
            _serviceFactory.GetRequiredService<ISecurityPolicyProvider>(),
            _serviceFactory.GetRequiredService<IHashProviderFactory>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<ILogger<BasicAuthProcessor>>()
        );
    }

    [Fact]
    public async Task DeleteBasicAuthAsync_ReturnsSuccess_WhenBasicAuthExistsAndFederatedIdentityExists()
    {
        var userId = Guid.CreateVersion7();
        await _basicAuthProcessor.CreateBasicAuthAsync(
            new CreateBasicAuthRequest(userId, VALID_PASSWORD)
        );

        var federatedIdentityRepo = _serviceFactory.GetRequiredService<
            IRepo<FederatedIdentityEntity>
        >();
        await federatedIdentityRepo.CreateAsync(
            new FederatedIdentityEntity
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                ProviderType = IdentityProviderType.Google,
                Issuer = "https://accounts.google.com",
                Subject = "google-subject-123",
                Email = "test@gmail.com",
            }
        );

        var result = await _basicAuthProcessor.DeleteBasicAuthAsync(userId);

        Assert.Equal(DeleteBasicAuthResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task DeleteBasicAuthAsync_ReturnsNotFoundError_WhenNoBasicAuth()
    {
        var userId = Guid.CreateVersion7();

        var result = await _basicAuthProcessor.DeleteBasicAuthAsync(userId);

        Assert.Equal(DeleteBasicAuthResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task DeleteBasicAuthAsync_ReturnsLastAuthMethodError_WhenNoFederatedIdentity()
    {
        var userId = Guid.CreateVersion7();
        await _basicAuthProcessor.CreateBasicAuthAsync(
            new CreateBasicAuthRequest(userId, VALID_PASSWORD)
        );

        var result = await _basicAuthProcessor.DeleteBasicAuthAsync(userId);

        Assert.Equal(DeleteBasicAuthResultCode.LastAuthMethodError, result.ResultCode);
    }
}
