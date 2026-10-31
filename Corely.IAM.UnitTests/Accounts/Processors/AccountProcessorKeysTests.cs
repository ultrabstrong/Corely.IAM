using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Invitations.Entities;
using Corely.IAM.Models;
using Corely.IAM.Platform.Providers;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Accounts.Processors;

public class AccountProcessorKeysTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly AccountProcessor _accountProcessor;
    private readonly Guid _accountId = Guid.CreateVersion7();

    public AccountProcessorKeysTests()
    {
        _accountProcessor = new AccountProcessor(
            _serviceFactory.GetRequiredService<IRepo<AccountEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<UserEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<InvitationEntity>>(),
            _serviceFactory.GetRequiredService<IUserOwnershipProcessor>(),
            _serviceFactory.GetRequiredService<ISecurityProvider>(),
            _serviceFactory.GetRequiredService<IUserContextProvider>(),
            _serviceFactory.GetRequiredService<IPlatformAccessProvider>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<TimeProvider>(),
            _serviceFactory.GetRequiredService<ILogger<AccountProcessor>>()
        );
    }

    private Task CreateAccountAsync() =>
        _serviceFactory
            .GetRequiredService<IRepo<AccountEntity>>()
            .CreateAsync(new AccountEntity { Id = _accountId, AccountName = "keys" });

    private void SetContext(UserContext context) =>
        _serviceFactory.GetRequiredService<IUserContextSetter>().SetUserContext(context);

    private static UserContext UserIn(params Account[] accounts) =>
        new(
            new User { Id = Guid.CreateVersion7(), Username = "someone" },
            accounts.FirstOrDefault(),
            "device",
            [.. accounts]
        );

    [Fact]
    public async Task GetAccountKeys_ReturnsTheAccount_ForSystemContext()
    {
        await CreateAccountAsync();
        SetContext(new UserContext(true, "system"));

        var result = await _accountProcessor.GetAccountKeysAsync(_accountId);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.Equal(_accountId, result.Data!.Id);
    }

    [Fact]
    public async Task GetAccountKeys_ReturnsNotFound_ForSystemContextAndAMissingAccount()
    {
        SetContext(new UserContext(true, "system"));

        var result = await _accountProcessor.GetAccountKeysAsync(_accountId);

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetAccountKeys_ReturnsTheAccount_ForAUserWhoBelongsToIt()
    {
        await CreateAccountAsync();
        SetContext(UserIn(new Account { Id = _accountId, AccountName = "keys" }));

        var result = await _accountProcessor.GetAccountKeysAsync(_accountId);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task GetAccountKeys_RefusesAUser_ForAnAccountOutsideTheirList()
    {
        await CreateAccountAsync();
        SetContext(UserIn(new Account { Id = Guid.CreateVersion7(), AccountName = "theirs" }));

        var result = await _accountProcessor.GetAccountKeysAsync(_accountId);

        Assert.Equal(RetrieveResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.Data);
    }
}
