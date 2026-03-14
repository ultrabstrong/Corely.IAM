using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.GoogleAuths.Entities;
using Corely.IAM.Validators;
using Corely.Security.Hashing.Factories;
using Corely.Security.PasswordValidation.Providers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.BasicAuths.Processors;

public class BasicAuthProcessorDeleteTests
{
    private const string VALID_PASSWORD = "Password1!";

    private readonly ServiceFactory _serviceFactory = new();
    private readonly BasicAuthProcessor _basicAuthProcessor;

    public BasicAuthProcessorDeleteTests()
    {
        _basicAuthProcessor = new BasicAuthProcessor(
            _serviceFactory.GetRequiredService<IRepo<BasicAuthEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<GoogleAuthEntity>>(),
            _serviceFactory.GetRequiredService<IPasswordValidationProvider>(),
            _serviceFactory.GetRequiredService<IHashProviderFactory>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<ILogger<BasicAuthProcessor>>()
        );
    }

    [Fact]
    public async Task DeleteBasicAuthAsync_ReturnsSuccess_WhenBasicAuthExistsAndGoogleAuthExists()
    {
        var userId = Guid.CreateVersion7();
        await _basicAuthProcessor.CreateBasicAuthAsync(
            new CreateBasicAuthRequest(userId, VALID_PASSWORD)
        );

        var googleAuthRepo = _serviceFactory.GetRequiredService<IRepo<GoogleAuthEntity>>();
        await googleAuthRepo.CreateAsync(
            new GoogleAuthEntity
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                GoogleSubjectId = "google-subject-123",
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
    public async Task DeleteBasicAuthAsync_ReturnsLastAuthMethodError_WhenNoGoogleAuth()
    {
        var userId = Guid.CreateVersion7();
        await _basicAuthProcessor.CreateBasicAuthAsync(
            new CreateBasicAuthRequest(userId, VALID_PASSWORD)
        );

        var result = await _basicAuthProcessor.DeleteBasicAuthAsync(userId);

        Assert.Equal(DeleteBasicAuthResultCode.LastAuthMethodError, result.ResultCode);
    }
}
