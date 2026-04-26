using Corely.IAM.PasswordRecoveries.Models;
using Corely.IAM.PasswordRecoveries.Processors;
using Corely.IAM.Services;

namespace Corely.IAM.UnitTests.Services;

public class PasswordRecoveryServiceTests
{
    private readonly Mock<IPasswordRecoveryProcessor> _passwordRecoveryProcessorMock = new();
    private readonly PasswordRecoveryService _service;

    public PasswordRecoveryServiceTests()
    {
        _service = new PasswordRecoveryService(_passwordRecoveryProcessorMock.Object);
    }

    [Fact]
    public async Task RequestPasswordRecoveryAsync_DelegatesToProcessor()
    {
        var request = new RequestPasswordRecoveryRequest("user@example.com");
        var expected = new RequestPasswordRecoveryResult(
            RequestPasswordRecoveryResultCode.Success,
            string.Empty,
            "token"
        );
        _passwordRecoveryProcessorMock
            .Setup(x => x.RequestPasswordRecoveryAsync(request))
            .ReturnsAsync(expected);

        var result = await _service.RequestPasswordRecoveryAsync(request);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ValidatePasswordRecoveryTokenAsync_DelegatesToProcessor()
    {
        var request = new ValidatePasswordRecoveryTokenRequest("token");
        var expected = new ValidatePasswordRecoveryTokenResult(
            ValidatePasswordRecoveryTokenResultCode.Success,
            string.Empty
        );
        _passwordRecoveryProcessorMock
            .Setup(x => x.ValidatePasswordRecoveryTokenAsync(request))
            .ReturnsAsync(expected);

        var result = await _service.ValidatePasswordRecoveryTokenAsync(request);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ResetPasswordWithRecoveryAsync_DelegatesToProcessor()
    {
        var request = new ResetPasswordWithRecoveryRequest("token", "password");
        var expected = new ResetPasswordWithRecoveryResult(
            ResetPasswordWithRecoveryResultCode.Success,
            string.Empty
        );
        _passwordRecoveryProcessorMock
            .Setup(x => x.ResetPasswordWithRecoveryAsync(request))
            .ReturnsAsync(expected);

        var result = await _service.ResetPasswordWithRecoveryAsync(request);

        Assert.Equal(expected, result);
    }
}
