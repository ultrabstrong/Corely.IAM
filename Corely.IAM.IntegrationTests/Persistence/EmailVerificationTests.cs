using Corely.IAM.EmailVerifications.Entities;
using Corely.IAM.EmailVerifications.Models;
using Corely.IAM.EmailVerifications.Processors;
using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
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

    [Fact]
    public async Task TakeOver_RemovesMemberships_ButKeepsAnAccountTheHolderAloneOwns()
    {
        var ownAccountId = await _scenario.ActAsAsync(
            _scenario.OutsiderUsername,
            null,
            async services =>
            {
                var own = await services
                    .GetRequiredService<IRegistrationService>()
                    .RegisterAccountAsync(
                        new RegisterAccountRequest("Outsider's own", _scenario.OutsiderUserId)
                    );
                Assert.Equal(RegisterAccountResultCode.Success, own.ResultCode);
                return own.CreatedAccountId;
            }
        );
        await _scenario.AsSystemAsync(async services =>
        {
            var joined = await services
                .GetRequiredService<IRegistrationService>()
                .RegisterUserWithAccountAsync(
                    new RegisterUserWithAccountRequest(
                        _scenario.OutsiderUserId,
                        _scenario.AccountId
                    )
                );
            Assert.Equal(RegisterUserWithAccountResultCode.Success, joined.ResultCode);
            return joined;
        });

        await _scenario.Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IEmailVerificationProcessor>()
                .MarkVerifiedAsync(_scenario.OutsiderUserId, EmailProof.IdentityProviderClaim)
        );

        var accountIds = await _scenario.Host.QueryAsync(db =>
            db.Set<UserEntity>()
                .Where(u => u.Id == _scenario.OutsiderUserId)
                .SelectMany(u => u.Accounts!.Select(a => a.Id))
                .ToListAsync()
        );
        Assert.Equal([ownAccountId], accountIds);
    }

    private Task<VerifyEmailResult> VerifyAsync(string token) =>
        _scenario.Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IEmailVerificationService>()
                .VerifyEmailAsync(new VerifyEmailRequest(token))
        );
}
