using Corely.IAM.EmailVerifications.Entities;
using Corely.IAM.EmailVerifications.Models;
using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Services;
using Corely.IAM.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Persistence;

public class EmailVerificationTests : IAsyncLifetime
{
    private readonly IamScenario _scenario = new();

    public ValueTask InitializeAsync() => _scenario.InitializeAsync();

    public ValueTask DisposeAsync() => _scenario.DisposeAsync();

    private Task<RequestEmailVerificationResult> RequestAsync() =>
        _scenario.AsSystemAsync(services =>
            services
                .GetRequiredService<IEmailVerificationService>()
                .RequestEmailVerificationForUserAsync(_scenario.OutsiderUserId)
        );

    [Fact]
    public async Task RequestEmailVerification_StoresOnlyTheSecretsHash()
    {
        var result = await RequestAsync();

        var secret = result.Token!.Split('.')[1];
        var stored = await _scenario.Host.QueryAsync(db =>
            db.Set<EmailVerificationEntity>().AsNoTracking().SingleAsync()
        );
        Assert.DoesNotContain(secret, stored.SecretHash);
        Assert.Equal(VerifyEmailResultCode.Success, (await VerifyAsync(result.Token)).ResultCode);
    }

    [Fact]
    public async Task DeletingAUser_DeletesItsEmailVerifications()
    {
        await RequestAsync();

        await _scenario.Host.QueryAsync(async db =>
        {
            db.Remove(
                await db.Set<UserEntity>().SingleAsync(u => u.Id == _scenario.OutsiderUserId)
            );
            return await db.SaveChangesAsync();
        });

        Assert.Equal(
            0,
            await _scenario.Host.QueryAsync(db => db.Set<EmailVerificationEntity>().CountAsync())
        );
    }

    private Task<VerifyEmailResult> VerifyAsync(string token) =>
        _scenario.Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IEmailVerificationService>()
                .VerifyEmailAsync(new VerifyEmailRequest(token))
        );
}
