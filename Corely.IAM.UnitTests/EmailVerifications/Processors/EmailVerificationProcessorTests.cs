using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.EmailVerifications.Entities;
using Corely.IAM.EmailVerifications.Models;
using Corely.IAM.EmailVerifications.Processors;
using Corely.IAM.EmailVerifications.Providers;
using Corely.IAM.FederatedIdentities.Entities;
using Corely.IAM.Platform.Models;
using Corely.IAM.TotpAuths.Entities;
using Corely.IAM.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Corely.IAM.UnitTests.EmailVerifications.Processors;

public class EmailVerificationProcessorTests
{
    private const string EMAIL = "owner@example.com";

    private readonly Clock _clock = new();
    private readonly List<EmailVerificationDelivery> _sent = [];
    private ServiceFactory _factory;

    public EmailVerificationProcessorTests()
    {
        _factory = Factory(withSender: true);
    }

    private ServiceFactory Factory(bool withSender) =>
        new(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(_clock);
            if (withSender)
                services.AddSingleton<IEmailVerificationSender>(new Sender(_sent));
        });

    private IEmailVerificationProcessor Processor =>
        _factory.GetRequiredService<IEmailVerificationProcessor>();

    private IRepo<T> Repo<T>()
        where T : class => _factory.GetRequiredService<IRepo<T>>();

    private async Task<UserEntity> CreateUserAsync(
        string email = EMAIL,
        DateTime? emailVerifiedUtc = null
    ) =>
        await Repo<UserEntity>()
            .CreateAsync(
                new UserEntity
                {
                    Id = Guid.CreateVersion7(),
                    Username = $"user{Guid.NewGuid():N}"[..20],
                    Email = email,
                    EmailVerifiedUtc = emailVerifiedUtc,
                }
            );

    private Task<UserEntity?> UserAsync(Guid id) => Repo<UserEntity>().GetAsync(u => u.Id == id);

    [Fact]
    public async Task RequestForUserAsync_SendsALinkThroughTheSender_ForAnUnconfirmedUser()
    {
        var user = await CreateUserAsync();

        var result = await Processor.RequestForUserAsync(user.Id);

        Assert.Equal(RequestEmailVerificationResultCode.Success, result.ResultCode);
        Assert.Equal(EmailVerificationDeliveryStatus.Sent, result.Delivery);
        Assert.Null(result.Token);
        var delivery = Assert.Single(_sent);
        Assert.Equal(EMAIL, delivery.Email);
        var stored = await Repo<EmailVerificationEntity>().GetAsync(v => v.UserId == user.Id);
        Assert.DoesNotContain(delivery.Token.Split('.')[1], stored!.SecretHash);
    }

    [Fact]
    public async Task RequestForUserAsync_ReturnsTheToken_ForAHostWithNoSender()
    {
        _factory = Factory(withSender: false);
        var user = await CreateUserAsync();

        var result = await Processor.RequestForUserAsync(user.Id);

        Assert.Equal(EmailVerificationDeliveryStatus.NotAttempted, result.Delivery);
        Assert.NotNull(result.Token);
    }

    [Fact]
    public async Task RequestForUserAsync_RefusesAConfirmedEmail()
    {
        var user = await CreateUserAsync(emailVerifiedUtc: _clock.Now);

        var result = await Processor.RequestForUserAsync(user.Id);

        Assert.Equal(RequestEmailVerificationResultCode.AlreadyVerifiedError, result.ResultCode);
    }

    [Fact]
    public async Task RequestForUserAsync_HoldsTheNextLinkBack_ForTheCooldown()
    {
        var user = await CreateUserAsync();
        await Processor.RequestForUserAsync(user.Id);

        var early = await Processor.RequestForUserAsync(user.Id);
        _clock.Advance(SecurityPolicy.EMAIL_VERIFICATION_RESEND_SECONDS_DEFAULT);
        var later = await Processor.RequestForUserAsync(user.Id);

        Assert.Equal(RequestEmailVerificationResultCode.ResendCooldownError, early.ResultCode);
        Assert.NotNull(early.NextResendUtc);
        Assert.Equal(RequestEmailVerificationResultCode.Success, later.ResultCode);
        Assert.Equal(
            VerifyEmailResultCode.EmailVerificationInvalidatedError,
            (await Processor.VerifyEmailAsync(new(_sent[0].Token))).ResultCode
        );
    }

    [Fact]
    public async Task VerifyEmailAsync_ConfirmsTheEmail_AndStillSaysConfirmed_WhenOpenedTwice()
    {
        var user = await CreateUserAsync();
        await Processor.RequestForUserAsync(user.Id);

        var first = await Processor.VerifyEmailAsync(new(_sent[0].Token));
        var second = await Processor.VerifyEmailAsync(new(_sent[0].Token));

        Assert.Equal(VerifyEmailResultCode.Success, first.ResultCode);
        Assert.Equal(VerifyEmailResultCode.Success, second.ResultCode);
        Assert.NotNull((await UserAsync(user.Id))!.EmailVerifiedUtc);
    }

    [Fact]
    public async Task VerifyEmailAsync_RefusesAnExpiredLink()
    {
        var user = await CreateUserAsync();
        await Processor.RequestForUserAsync(user.Id);
        _clock.Advance(SecurityPolicy.EMAIL_VERIFICATION_TTL_SECONDS_DEFAULT);

        var result = await Processor.VerifyEmailAsync(new(_sent[0].Token));

        Assert.Equal(VerifyEmailResultCode.EmailVerificationExpiredError, result.ResultCode);
        Assert.Null((await UserAsync(user.Id))!.EmailVerifiedUtc);
    }

    [Fact]
    public async Task VerifyEmailAsync_RefusesALink_ForAnEmailThatHasSinceChanged()
    {
        var user = await CreateUserAsync();
        await Processor.RequestForUserAsync(user.Id);
        user.Email = "someone.else@example.com";
        await Repo<UserEntity>().UpdateAsync(user);

        var result = await Processor.VerifyEmailAsync(new(_sent[0].Token));

        Assert.Equal(VerifyEmailResultCode.EmailChangedError, result.ResultCode);
    }

    [Fact]
    public async Task VerifyEmailAsync_RefusesATamperedSecret()
    {
        var user = await CreateUserAsync();
        await Processor.RequestForUserAsync(user.Id);
        var id = _sent[0].Token.Split('.')[0];

        var result = await Processor.VerifyEmailAsync(new($"{id}.not-the-secret"));

        Assert.Equal(VerifyEmailResultCode.EmailVerificationNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task VerifyEmailAsync_KeepsEveryCredential_SinceTheLinkConfirmsTheUserAsItStands()
    {
        var user = await CreateUserAsync();
        await Repo<BasicAuthEntity>()
            .CreateAsync(
                new BasicAuthEntity
                {
                    Id = Guid.CreateVersion7(),
                    UserId = user.Id,
                    Password = "hash",
                }
            );
        await Processor.RequestForUserAsync(user.Id);

        await Processor.VerifyEmailAsync(new(_sent[0].Token));

        Assert.True(await Repo<BasicAuthEntity>().AnyAsync(b => b.UserId == user.Id));
    }

    [Fact]
    public async Task RejectAsync_RemovesAnUnconfirmedUser_ForTheOwnerOfTheAddress()
    {
        var user = await CreateUserAsync();
        await Processor.RequestForUserAsync(user.Id);

        var result = await Processor.RejectAsync(new(_sent.Single().Token));

        Assert.Equal(RejectEmailVerificationResultCode.Success, result.ResultCode);
        Assert.True(result.Removed);
        Assert.Null(await UserAsync(user.Id));
    }

    [Fact]
    public async Task RejectAsync_KeepsAConfirmedUser()
    {
        var user = await CreateUserAsync();
        await Processor.RequestForUserAsync(user.Id);
        await Processor.VerifyEmailAsync(new(_sent.Single().Token));

        var result = await Processor.RejectAsync(new(_sent.Single().Token));

        Assert.Equal(RejectEmailVerificationResultCode.AlreadyConfirmedError, result.ResultCode);
        Assert.NotNull(await UserAsync(user.Id));
    }

    [Fact]
    public async Task RejectAsync_RefusesATamperedSecret()
    {
        var user = await CreateUserAsync();
        await Processor.RequestForUserAsync(user.Id);
        var id = _sent.Single().Token.Split('.')[0];

        var result = await Processor.RejectAsync(new($"{id}.not-the-secret"));

        Assert.Equal(
            RejectEmailVerificationResultCode.EmailVerificationNotFoundError,
            result.ResultCode
        );
        Assert.NotNull(await UserAsync(user.Id));
    }

    [Theory]
    [InlineData(EmailProof.IdentityProviderClaim, false)]
    [InlineData(EmailProof.PasswordRecovery, true)]
    public async Task MarkVerifiedAsync_TakesOverAnUnconfirmedUser_ForProofFromSomeoneElse(
        Enum proof,
        bool keepsPassword
    )
    {
        var user = await CreateUserAsync();
        var account = await Repo<AccountEntity>()
            .CreateAsync(new AccountEntity { Id = Guid.CreateVersion7(), AccountName = "joined" });
        var holder = await Repo<UserEntity>()
            .GetAsync(u => u.Id == user.Id, include: q => q.Include(u => u.Accounts));
        holder!.Accounts = [account];
        await Repo<UserEntity>().UpdateAsync(holder);
        await Repo<BasicAuthEntity>()
            .CreateAsync(
                new BasicAuthEntity
                {
                    Id = Guid.CreateVersion7(),
                    UserId = user.Id,
                    Password = "hash",
                }
            );
        await Repo<TotpAuthEntity>()
            .CreateAsync(
                new TotpAuthEntity
                {
                    Id = Guid.CreateVersion7(),
                    UserId = user.Id,
                    EncryptedSecret = "s",
                }
            );
        await Repo<FederatedIdentityEntity>()
            .CreateAsync(
                new FederatedIdentityEntity
                {
                    Id = Guid.CreateVersion7(),
                    UserId = user.Id,
                    Issuer = "squatter",
                    Subject = "squatter",
                }
            );

        await Processor.MarkVerifiedAsync(user.Id, (EmailProof)proof);

        Assert.Equal(
            keepsPassword,
            await Repo<BasicAuthEntity>().AnyAsync(b => b.UserId == user.Id)
        );
        Assert.False(await Repo<TotpAuthEntity>().AnyAsync(t => t.UserId == user.Id));
        Assert.False(await Repo<FederatedIdentityEntity>().AnyAsync(f => f.UserId == user.Id));
        var after = await Repo<UserEntity>()
            .GetAsync(u => u.Id == user.Id, include: q => q.Include(u => u.Accounts));
        Assert.Empty(after!.Accounts!);
        Assert.NotNull(after.EmailVerifiedUtc);
    }

    [Theory]
    [InlineData(EmailProof.LinkedIdentity)]
    [InlineData(EmailProof.Operator)]
    public async Task MarkVerifiedAsync_KeepsEveryCredential_ForProofFromTheUserItself(Enum proof)
    {
        var user = await CreateUserAsync();
        await Repo<BasicAuthEntity>()
            .CreateAsync(
                new BasicAuthEntity
                {
                    Id = Guid.CreateVersion7(),
                    UserId = user.Id,
                    Password = "hash",
                }
            );

        await Processor.MarkVerifiedAsync(user.Id, (EmailProof)proof);

        Assert.True(await Repo<BasicAuthEntity>().AnyAsync(b => b.UserId == user.Id));
        Assert.NotNull((await UserAsync(user.Id))!.EmailVerifiedUtc);
    }

    [Fact]
    public async Task MarkVerifiedAsync_TakesNothingOver_ForAConfirmedUser()
    {
        var user = await CreateUserAsync(emailVerifiedUtc: _clock.Now);
        await Repo<BasicAuthEntity>()
            .CreateAsync(
                new BasicAuthEntity
                {
                    Id = Guid.CreateVersion7(),
                    UserId = user.Id,
                    Password = "hash",
                }
            );

        await Processor.MarkVerifiedAsync(user.Id, EmailProof.IdentityProviderClaim);

        Assert.True(await Repo<BasicAuthEntity>().AnyAsync(b => b.UserId == user.Id));
    }

    [Fact]
    public async Task ReplaceUnconfirmedHolderAsync_DeletesTheHolder_WhileConfirmationIsRequired()
    {
        var user = await CreateUserAsync();

        Assert.True(await Processor.ReplaceUnconfirmedHolderAsync(EMAIL));
        Assert.Null(await UserAsync(user.Id));
    }

    [Fact]
    public async Task ReplaceUnconfirmedHolderAsync_KeepsTheHolder_ForAHostWithNoSender()
    {
        _factory = Factory(withSender: false);
        var user = await CreateUserAsync();

        Assert.False(await Processor.ReplaceUnconfirmedHolderAsync(EMAIL));
        Assert.NotNull(await UserAsync(user.Id));
    }

    [Fact]
    public async Task ReplaceUnconfirmedHolderAsync_KeepsAConfirmedHolder()
    {
        var user = await CreateUserAsync(emailVerifiedUtc: _clock.Now);

        Assert.False(await Processor.ReplaceUnconfirmedHolderAsync(EMAIL));
        Assert.NotNull(await UserAsync(user.Id));
    }

    [Fact]
    public async Task MustConfirmEmailAsync_IsTrue_OnlyForAnUnconfirmedUser_WhileASenderIsRegistered()
    {
        var unconfirmed = await CreateUserAsync();
        var confirmed = await CreateUserAsync("other@example.com", _clock.Now);

        Assert.True(await Processor.MustConfirmEmailAsync(unconfirmed.Id));
        Assert.False(await Processor.MustConfirmEmailAsync(confirmed.Id));

        _factory = Factory(withSender: false);
        var noSender = await CreateUserAsync();
        Assert.False(await Processor.MustConfirmEmailAsync(noSender.Id));
    }

    [Fact]
    public async Task CreateChallengeAsync_SendsALink_AndCompletesOnlyOnceConfirmed()
    {
        var user = await CreateUserAsync();

        var challenge = await Processor.CreateChallengeAsync(user.Id, "device", null);
        var pending = await Processor.CompleteChallengeAsync(challenge);
        await Processor.VerifyEmailAsync(new(_sent.Single().Token));
        var confirmed = await Processor.CompleteChallengeAsync(challenge);
        var again = await Processor.CompleteChallengeAsync(challenge);

        Assert.Equal(EmailVerificationChallengeOutcomeCode.Pending, pending.ResultCode);
        Assert.Equal(EmailVerificationChallengeOutcomeCode.Confirmed, confirmed.ResultCode);
        Assert.Equal(user.Id, confirmed.UserId);
        Assert.Equal("device", confirmed.DeviceId);
        Assert.Equal(EmailVerificationChallengeOutcomeCode.NotFound, again.ResultCode);
    }

    [Fact]
    public async Task CreateChallengeAsync_ReusesALinkStillInsideItsCooldown()
    {
        var user = await CreateUserAsync();

        await Processor.CreateChallengeAsync(user.Id, "device", null);
        await Processor.CreateChallengeAsync(user.Id, "device", null);

        Assert.Single(_sent);
    }

    [Fact]
    public async Task RequestForChallengeAsync_SendsANewLink_AndKeepsTheChallengeAliveForIt()
    {
        var user = await CreateUserAsync();
        var challenge = await Processor.CreateChallengeAsync(user.Id, "device", null);
        _clock.Advance(SecurityPolicy.EMAIL_VERIFICATION_TTL_SECONDS_DEFAULT * 2 - 1);

        var resent = await Processor.RequestForChallengeAsync(challenge);
        _clock.Advance(SecurityPolicy.EMAIL_VERIFICATION_TTL_SECONDS_DEFAULT);
        var status = await Processor.GetStatusAsync(challenge);

        Assert.Equal(RequestEmailVerificationResultCode.Success, resent.ResultCode);
        Assert.Equal(GetEmailVerificationStatusResultCode.Success, status.ResultCode);
        Assert.Equal(EmailVerificationState.Expired, status.Status!.State);
    }

    [Fact]
    public async Task GetStatusAsync_ReportsEachState()
    {
        var user = await CreateUserAsync();
        var challenge = await Processor.CreateChallengeAsync(user.Id, "device", null);

        var sent = await Processor.GetStatusAsync(challenge);
        await Processor.VerifyEmailAsync(new(_sent.Single().Token));
        var confirmed = await Processor.GetStatusAsync(challenge);
        var unknown = await Processor.GetStatusAsync("not-a-challenge");

        Assert.Equal(EmailVerificationState.Sent, sent.Status!.State);
        Assert.Equal(EMAIL, sent.Status.Email);
        Assert.Equal(EmailVerificationState.Confirmed, confirmed.Status!.State);
        Assert.Equal(
            GetEmailVerificationStatusResultCode.ChallengeNotFoundError,
            unknown.ResultCode
        );
    }

    private sealed class Sender(List<EmailVerificationDelivery> sent) : IEmailVerificationSender
    {
        public Task<EmailVerificationSendResult> SendAsync(EmailVerificationDelivery delivery)
        {
            sent.Add(delivery);
            return Task.FromResult(
                new EmailVerificationSendResult(EmailVerificationSendResultCode.Sent)
            );
        }
    }

    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        public DateTime Now => _now.UtcDateTime;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(int seconds) => _now = _now.AddSeconds(seconds);
    }
}
