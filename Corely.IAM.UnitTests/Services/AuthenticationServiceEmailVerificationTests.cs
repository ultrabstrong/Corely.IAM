using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.ApiKeys.Providers;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.EmailVerifications.Models;
using Corely.IAM.EmailVerifications.Processors;
using Corely.IAM.EmailVerifications.Providers;
using Corely.IAM.MfaChallenges.Entities;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Processors;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Services;

public class AuthenticationServiceEmailVerificationTests
{
    private const string DEVICE = "device";
    private const string CHALLENGE = "challenge-token";

    private readonly ServiceFactory _serviceFactory = new();
    private readonly Mock<IEmailVerificationProcessor> _emailVerification = new();
    private readonly Mock<ITotpAuthProcessor> _totp = new();
    private readonly Mock<IAuthenticationProvider> _authenticationProvider = new();

    public AuthenticationServiceEmailVerificationTests()
    {
        _emailVerification
            .Setup(p =>
                p.CreateChallengeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>())
            )
            .ReturnsAsync(CHALLENGE);
        _authenticationProvider
            .Setup(p => p.GetUserAuthTokenAsync(It.IsAny<GetUserAuthTokenRequest>()))
            .ReturnsAsync(
                new UserAuthTokenResult(
                    UserAuthTokenResultCode.Success,
                    "token",
                    Guid.CreateVersion7(),
                    new User { Id = Guid.CreateVersion7() },
                    null,
                    []
                )
            );
    }

    private AuthenticationService Service(bool requireVerifiedEmail = true)
    {
        var basicAuth = new Mock<IBasicAuthProcessor>();
        basicAuth
            .Setup(b => b.VerifyBasicAuthAsync(It.IsAny<VerifyBasicAuthRequest>()))
            .ReturnsAsync(new VerifyBasicAuthResult(VerifyBasicAuthResultCode.Success, "", true));
        var policy = FixedSecurityPolicyProvider.From(
            new SecurityOptions { RequireVerifiedEmail = requireVerifiedEmail }
        );

        return new AuthenticationService(
            _serviceFactory.GetRequiredService<ILogger<AuthenticationService>>(),
            _serviceFactory.GetRequiredService<IRepo<UserEntity>>(),
            _authenticationProvider.Object,
            new Mock<IUserContextProvider>().Object,
            new Mock<IUserContextSetter>().Object,
            new Mock<IAuthorizationCacheClearer>().Object,
            basicAuth.Object,
            _totp.Object,
            _serviceFactory.GetRequiredService<IRepo<MfaChallengeEntity>>(),
            policy,
            new Mock<IApiKeyAuthenticationProvider>().Object,
            new Mock<ISessionTokenProvider>().Object,
            _emailVerification.Object,
            new EmailVerificationRequirement(policy, new Mock<IEmailVerificationSender>().Object),
            TimeProvider.System
        );
    }

    private async Task<UserEntity> CreateUserAsync(DateTime? emailVerifiedUtc = null) =>
        await _serviceFactory
            .GetRequiredService<IRepo<UserEntity>>()
            .CreateAsync(
                new UserEntity
                {
                    Id = Guid.CreateVersion7(),
                    Username = $"user{Guid.NewGuid():N}"[..20],
                    Email = $"{Guid.NewGuid():N}@example.com",
                    EmailVerifiedUtc = emailVerifiedUtc,
                }
            );

    [Fact]
    public async Task SignInAsync_ReturnsAnEmailChallenge_ForAnUnconfirmedUser()
    {
        var user = await CreateUserAsync();

        var result = await Service().SignInAsync(new SignInRequest(user.Username, "pw", DEVICE));

        Assert.Equal(SignInResultCode.EmailVerificationRequiredChallenge, result.ResultCode);
        Assert.Equal(CHALLENGE, result.EmailVerificationChallengeToken);
        Assert.Null(result.AuthToken);
    }

    [Fact]
    public async Task SignInAsync_SignsIn_ForAConfirmedUser()
    {
        var user = await CreateUserAsync(DateTime.UtcNow);

        var result = await Service().SignInAsync(new SignInRequest(user.Username, "pw", DEVICE));

        Assert.Equal(SignInResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task SignInAsync_SignsIn_ForAnUnconfirmedUser_WhenConfirmationIsOff()
    {
        var user = await CreateUserAsync();

        var result = await Service(requireVerifiedEmail: false)
            .SignInAsync(new SignInRequest(user.Username, "pw", DEVICE));

        Assert.Equal(SignInResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task SignInAsync_AsksForTheCodeFirst_ForAnUnconfirmedUserWithTwoFactor()
    {
        var user = await CreateUserAsync();
        _totp.Setup(t => t.IsTotpEnabledAsync(user.Id)).ReturnsAsync(true);

        var result = await Service().SignInAsync(new SignInRequest(user.Username, "pw", DEVICE));

        Assert.Equal(SignInResultCode.MfaRequiredChallenge, result.ResultCode);
        _emailVerification.Verify(
            p => p.CreateChallengeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>()),
            Times.Never
        );
    }

    [Theory]
    [InlineData(
        EmailVerificationChallengeOutcomeCode.Pending,
        SignInResultCode.EmailVerificationPendingError
    )]
    [InlineData(
        EmailVerificationChallengeOutcomeCode.NotFound,
        SignInResultCode.EmailVerificationChallengeExpiredError
    )]
    [InlineData(EmailVerificationChallengeOutcomeCode.Confirmed, SignInResultCode.Success)]
    public async Task CompleteEmailVerificationAsync_SignsIn_OnlyOnceTheEmailIsConfirmed(
        Enum outcome,
        SignInResultCode expected
    )
    {
        _emailVerification
            .Setup(p => p.CompleteChallengeAsync(CHALLENGE))
            .ReturnsAsync(
                new EmailVerificationChallengeOutcome(
                    (EmailVerificationChallengeOutcomeCode)outcome,
                    Guid.CreateVersion7(),
                    DEVICE
                )
            );

        var result = await Service()
            .CompleteEmailVerificationAsync(new CompleteEmailVerificationRequest(CHALLENGE));

        Assert.Equal(expected, result.ResultCode);
    }
}
