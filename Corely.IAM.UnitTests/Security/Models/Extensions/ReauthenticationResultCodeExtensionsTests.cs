using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Models.Extensions;

namespace Corely.IAM.UnitTests.Security.Models.Extensions;

public class ReauthenticationResultCodeExtensionsTests
{
    [Fact]
    public void ToModifyPasswordResultCode_MapsEveryValue_ToTheCodeOfTheSameName()
    {
        foreach (var value in Enum.GetValues<ReauthenticationResultCode>())
        {
            Assert.Equal(value.ToString(), value.ToModifyPasswordResultCode().ToString());
        }
    }

    [Fact]
    public void ToDeregisterBasicAuthResultCode_MapsEveryValue()
    {
        Assert.Equal(
            DeregisterBasicAuthResultCode.NotFoundError,
            ReauthenticationResultCode.BasicAuthNotFoundError.ToDeregisterBasicAuthResultCode()
        );
        foreach (
            var value in Enum.GetValues<ReauthenticationResultCode>()
                .Where(v => v != ReauthenticationResultCode.BasicAuthNotFoundError)
        )
        {
            Assert.Equal(value.ToString(), value.ToDeregisterBasicAuthResultCode().ToString());
        }
    }

    [Fact]
    public void ToRegisterPasswordResultCode_MapsTheTotpOutcomes()
    {
        Assert.Equal(
            RegisterPasswordResultCode.Success,
            ReauthenticationResultCode.Success.ToRegisterPasswordResultCode()
        );
        Assert.Equal(
            RegisterPasswordResultCode.TotpRequiredError,
            ReauthenticationResultCode.TotpRequiredError.ToRegisterPasswordResultCode()
        );
        Assert.Equal(
            RegisterPasswordResultCode.InvalidTotpCodeError,
            ReauthenticationResultCode.InvalidTotpCodeError.ToRegisterPasswordResultCode()
        );
    }

    [Fact]
    public void ToRegisterPasswordResultCode_Throws_ForAPasswordOutcome() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ReauthenticationResultCode.InvalidCurrentPasswordError.ToRegisterPasswordResultCode()
        );
}
