using AutoFixture;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Services;

public class RetrievalServiceTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly Fixture _fixture = new();
    private readonly Mock<IAccountProcessor> _accountProcessorMock;
    private readonly RetrievalService _retrievalService;

    private List<Account> _accountsForUser = [];

    public RetrievalServiceTests()
    {
        _accountProcessorMock = GetMockAccountProcessor();

        _retrievalService = new RetrievalService(
            _serviceFactory.GetRequiredService<ILogger<RetrievalService>>(),
            _accountProcessorMock.Object
        );
    }

    private Mock<IAccountProcessor> GetMockAccountProcessor()
    {
        var mock = new Mock<IAccountProcessor>();

        mock.Setup(m => m.GetAccountsForUserAsync(It.IsAny<int>()))
            .ReturnsAsync(() => _accountsForUser);

        return mock;
    }

    [Fact]
    public async Task RetrieveAccountsAsync_ReturnsAccounts_WhenUserHasAccounts()
    {
        var userId = _fixture.Create<int>();
        _accountsForUser =
        [
            new Account { Id = 1, AccountName = "Account1" },
            new Account { Id = 2, AccountName = "Account2" },
        ];
        var request = new RetrieveAccountsRequest(userId);

        var result = await _retrievalService.RetrieveAccountsAsync(request);

        Assert.Equal(2, result.Accounts.Count);
        Assert.Contains(result.Accounts, a => a.AccountName == "Account1");
        Assert.Contains(result.Accounts, a => a.AccountName == "Account2");
        _accountProcessorMock.Verify(m => m.GetAccountsForUserAsync(userId), Times.Once);
    }

    [Fact]
    public async Task RetrieveAccountsAsync_ReturnsEmptyList_WhenUserHasNoAccounts()
    {
        var userId = _fixture.Create<int>();
        _accountsForUser = [];
        var request = new RetrieveAccountsRequest(userId);

        var result = await _retrievalService.RetrieveAccountsAsync(request);

        Assert.Empty(result.Accounts);
        _accountProcessorMock.Verify(m => m.GetAccountsForUserAsync(userId), Times.Once);
    }

    [Fact]
    public async Task RetrieveAccountsAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _retrievalService.RetrieveAccountsAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }
}
