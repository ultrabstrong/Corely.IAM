using Corely.IAM.Accounts.Models;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.TotpAuths.Processors;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;

namespace Corely.IAM.UnitTests.Services;

public class MfaServiceTests
{
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Mock<ITotpAuthProcessor> _totpAuthProcessorMock = new();
    private readonly Mock<IUserContextProvider> _userContextProviderMock = new();
    private readonly MfaService _service;

    public MfaServiceTests()
    {
        var serviceFactory = new ServiceFactory();
        _userContextProviderMock
            .Setup(x => x.GetUserContext())
            .Returns(
                new UserContext(
                    new User { Id = _userId, Email = "user@test.com" },
                    new Account { Id = Guid.CreateVersion7() },
                    "device-1",
                    []
                )
            );

        _service = new MfaService(
            _totpAuthProcessorMock.Object,
            _userContextProviderMock.Object,
            serviceFactory.GetRequiredService<IValidationProvider>()
        );
    }

    [Fact]
    public async Task EnableTotpAsync_DelegatesToProcessor_WithCurrentUserContext()
    {
        var expected = new EnableTotpResult(
            EnableTotpResultCode.Success,
            string.Empty,
            "secret",
            "setup-uri",
            ["ABCD-EFGH"]
        );
        _totpAuthProcessorMock
            .Setup(x => x.EnableTotpAsync(_userId, "Corely.IAM", "user@test.com"))
            .ReturnsAsync(expected);

        var result = await _service.EnableTotpAsync();

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ConfirmTotpAsync_ReturnsInvalidCodeError_WhenRequestInvalid()
    {
        var result = await _service.ConfirmTotpAsync(new ConfirmTotpRequest(string.Empty));

        Assert.Equal(ConfirmTotpResultCode.InvalidCodeError, result.ResultCode);
        _totpAuthProcessorMock.Verify(
            x => x.ConfirmTotpAsync(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ConfirmTotpAsync_DelegatesToProcessor_WhenRequestValid()
    {
        var expected = new ConfirmTotpResult(ConfirmTotpResultCode.Success, string.Empty);
        _totpAuthProcessorMock
            .Setup(x => x.ConfirmTotpAsync(_userId, "123456"))
            .ReturnsAsync(expected);

        var result = await _service.ConfirmTotpAsync(new ConfirmTotpRequest("123456"));

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task DisableTotpAsync_ReturnsInvalidCodeError_WhenRequestInvalid()
    {
        var result = await _service.DisableTotpAsync(new DisableTotpRequest(string.Empty));

        Assert.Equal(DisableTotpResultCode.InvalidCodeError, result.ResultCode);
        _totpAuthProcessorMock.Verify(
            x => x.DisableTotpAsync(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task DisableTotpAsync_DelegatesToProcessor_WhenRequestValid()
    {
        var expected = new DisableTotpResult(DisableTotpResultCode.Success, string.Empty);
        _totpAuthProcessorMock
            .Setup(x => x.DisableTotpAsync(_userId, "123456"))
            .ReturnsAsync(expected);

        var result = await _service.DisableTotpAsync(new DisableTotpRequest("123456"));

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task RegenerateTotpRecoveryCodesAsync_DelegatesToProcessor()
    {
        var expected = new RegenerateTotpRecoveryCodesResult(
            RegenerateTotpRecoveryCodesResultCode.Success,
            string.Empty,
            ["ABCD-EFGH"]
        );
        _totpAuthProcessorMock
            .Setup(x => x.RegenerateTotpRecoveryCodesAsync(_userId))
            .ReturnsAsync(expected);

        var result = await _service.RegenerateTotpRecoveryCodesAsync();

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetTotpStatusAsync_DelegatesToProcessor()
    {
        var expected = new TotpStatusResult(TotpStatusResultCode.Success, string.Empty, true, 9);
        _totpAuthProcessorMock.Setup(x => x.GetTotpStatusAsync(_userId)).ReturnsAsync(expected);

        var result = await _service.GetTotpStatusAsync();

        Assert.Equal(expected, result);
    }
}
