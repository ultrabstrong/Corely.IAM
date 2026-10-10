using Corely.IAM.Models;
using Corely.IAM.Web.Extensions;

namespace Corely.IAM.Web.UnitTests.Extensions;

public class SignInResultCodeExtensionsTests
{
    [Fact]
    public void SwitchFailureMessage_AsksForTwoFactor_WhenTwoFactorIsRequired() =>
        Assert.Contains(
            "two factor",
            SignInResultCode.TwoFactorRequiredError.SwitchFailureMessage()
        );

    [Fact]
    public void SwitchFailureMessage_IsGeneric_ForAnyOtherFailure() =>
        Assert.Equal(
            "Failed to switch account.",
            SignInResultCode.AccountNotFoundError.SwitchFailureMessage()
        );
}
