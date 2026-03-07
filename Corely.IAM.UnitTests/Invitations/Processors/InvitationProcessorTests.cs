using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Invitations.Constants;
using Corely.IAM.Invitations.Entities;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Invitations.Processors;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Invitations.Processors;

public class InvitationProcessorTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly InvitationProcessor _invitationProcessor;

    public InvitationProcessorTests()
    {
        _invitationProcessor = new InvitationProcessor(
            _serviceFactory.GetRequiredService<IRepo<InvitationEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<AccountEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<UserEntity>>(),
            _serviceFactory.GetRequiredService<IUserContextProvider>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<TimeProvider>(),
            _serviceFactory.GetRequiredService<ILogger<InvitationProcessor>>()
        );
    }

    private async Task<UserEntity> CreateUserAsync()
    {
        var user = new UserEntity
        {
            Id = Guid.CreateVersion7(),
            Accounts = [],
            Groups = [],
            Roles = [],
        };
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        return await userRepo.CreateAsync(user);
    }

    private async Task<AccountEntity> CreateAccountAsync()
    {
        var account = new AccountEntity
        {
            Id = Guid.CreateVersion7(),
            AccountName = "testaccount",
            Users = [],
            Invitations = [],
        };
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        return await accountRepo.CreateAsync(account);
    }

    private void SetUserContext(UserEntity userEntity, AccountEntity? accountEntity = null)
    {
        var user = new User
        {
            Id = userEntity.Id,
            Username = "testuser",
            Email = "test@test.com",
        };
        Account? account =
            accountEntity != null
                ? new Account { Id = accountEntity.Id, AccountName = accountEntity.AccountName }
                : null;
        var accounts = account != null ? [account] : new List<Account>();
        var context = new UserContext(user, account, "device1", accounts);

        var setter = _serviceFactory.GetRequiredService<IUserContextSetter>();
        setter.SetUserContext(context);
    }

    private async Task<InvitationEntity> CreateInvitationEntityAsync(
        Guid accountId,
        Guid createdByUserId,
        string email = "invite@test.com",
        string? token = null,
        DateTime? expiresUtc = null,
        Guid? acceptedByUserId = null,
        DateTime? acceptedUtc = null,
        DateTime? revokedUtc = null
    )
    {
        var entity = new InvitationEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            Token = token ?? Guid.CreateVersion7().ToString("N"),
            CreatedByUserId = createdByUserId,
            Email = email,
            ExpiresUtc = expiresUtc ?? DateTime.UtcNow.AddDays(7),
            AcceptedByUserId = acceptedByUserId,
            AcceptedUtc = acceptedUtc,
            RevokedUtc = revokedUtc,
        };
        var repo = _serviceFactory.GetRequiredService<IRepo<InvitationEntity>>();
        return await repo.CreateAsync(entity);
    }

    // ─────────────────────────────────────────────
    // CreateInvitationAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CreateInvitationAsync_ReturnsSuccess_WithValidRequest()
    {
        var user = await CreateUserAsync();
        var account = await CreateAccountAsync();
        SetUserContext(user, account);

        var request = new CreateInvitationRequest(
            account.Id,
            "invite@example.com",
            "Join us",
            InvitationConstants.MIN_EXPIRY_SECONDS
        );

        var result = await _invitationProcessor.CreateInvitationAsync(request);

        Assert.Equal(CreateInvitationResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.InvitationId);
        Assert.NotEqual(Guid.Empty, result.InvitationId!.Value);
    }

    [Fact]
    public async Task CreateInvitationAsync_ReturnsAccountNotFound_WhenAccountDoesNotExist()
    {
        var user = await CreateUserAsync();
        SetUserContext(user);

        var request = new CreateInvitationRequest(
            Guid.CreateVersion7(),
            "invite@example.com",
            null,
            InvitationConstants.MIN_EXPIRY_SECONDS
        );

        var result = await _invitationProcessor.CreateInvitationAsync(request);

        Assert.Equal(CreateInvitationResultCode.AccountNotFoundError, result.ResultCode);
        Assert.Null(result.Token);
        Assert.Null(result.InvitationId);
    }

    // ─────────────────────────────────────────────
    // AcceptInvitationAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task AcceptInvitationAsync_ReturnsSuccess_AndAddsUserToAccount()
    {
        var creator = await CreateUserAsync();
        var acceptor = await CreateUserAsync();
        var account = await CreateAccountAsync();
        SetUserContext(creator, account);

        var createResult = await _invitationProcessor.CreateInvitationAsync(
            new CreateInvitationRequest(
                account.Id,
                "acceptor@test.com",
                null,
                InvitationConstants.MIN_EXPIRY_SECONDS
            )
        );

        SetUserContext(acceptor);

        var result = await _invitationProcessor.AcceptInvitationAsync(
            new AcceptInvitationRequest(createResult.Token!)
        );

        Assert.Equal(AcceptInvitationResultCode.Success, result.ResultCode);
        Assert.Equal(account.Id, result.AccountId);

        // Verify user was added to the account
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var accountEntity = await accountRepo.GetAsync(
            a => a.Id == account.Id,
            include: q => q.Include(a => a.Users)
        );
        Assert.NotNull(accountEntity?.Users);
        Assert.Contains(accountEntity.Users, u => u.Id == acceptor.Id);
    }

    [Fact]
    public async Task AcceptInvitationAsync_ReturnsSuccess_WhenUserAlreadyInAccount()
    {
        var user = await CreateUserAsync();
        var account = await CreateAccountAsync();

        // Add user to account first
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var accountEntity = await accountRepo.GetAsync(
            a => a.Id == account.Id,
            include: q => q.Include(a => a.Users)
        );
        accountEntity!.Users ??= [];
        accountEntity.Users.Add(user);
        await accountRepo.UpdateAsync(accountEntity);

        SetUserContext(user, account);

        var createResult = await _invitationProcessor.CreateInvitationAsync(
            new CreateInvitationRequest(
                account.Id,
                "user@test.com",
                null,
                InvitationConstants.MIN_EXPIRY_SECONDS
            )
        );

        var result = await _invitationProcessor.AcceptInvitationAsync(
            new AcceptInvitationRequest(createResult.Token!)
        );

        Assert.Equal(AcceptInvitationResultCode.Success, result.ResultCode);
        Assert.Equal(account.Id, result.AccountId);
    }

    [Fact]
    public async Task AcceptInvitationAsync_BurnsSiblingInvitations_ForSameAccountAndEmail()
    {
        var creator = await CreateUserAsync();
        var acceptor = await CreateUserAsync();
        var account = await CreateAccountAsync();
        SetUserContext(creator, account);

        var email = "sibling@test.com";

        // Create multiple invitations for same account+email
        var result1 = await _invitationProcessor.CreateInvitationAsync(
            new CreateInvitationRequest(
                account.Id,
                email,
                "First",
                InvitationConstants.MIN_EXPIRY_SECONDS
            )
        );
        var result2 = await _invitationProcessor.CreateInvitationAsync(
            new CreateInvitationRequest(
                account.Id,
                email,
                "Second",
                InvitationConstants.MIN_EXPIRY_SECONDS
            )
        );

        SetUserContext(acceptor);

        // Accept the first invitation
        var acceptResult = await _invitationProcessor.AcceptInvitationAsync(
            new AcceptInvitationRequest(result1.Token!)
        );
        Assert.Equal(AcceptInvitationResultCode.Success, acceptResult.ResultCode);

        // The second invitation should now be burned (accepted)
        var invitationRepo = _serviceFactory.GetRequiredService<IRepo<InvitationEntity>>();
        var siblingEntity = await invitationRepo.GetAsync(i => i.Id == result2.InvitationId);
        Assert.NotNull(siblingEntity);
        Assert.NotNull(siblingEntity.AcceptedByUserId);
        Assert.Equal(acceptor.Id, siblingEntity.AcceptedByUserId);
    }

    [Fact]
    public async Task AcceptInvitationAsync_ReturnsNotFound_WhenTokenIsEmpty()
    {
        var result = await _invitationProcessor.AcceptInvitationAsync(
            new AcceptInvitationRequest("")
        );

        Assert.Equal(AcceptInvitationResultCode.InvitationNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task AcceptInvitationAsync_ReturnsNotFound_WhenTokenDoesNotExist()
    {
        var result = await _invitationProcessor.AcceptInvitationAsync(
            new AcceptInvitationRequest("nonexistent-token")
        );

        Assert.Equal(AcceptInvitationResultCode.InvitationNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task AcceptInvitationAsync_ReturnsExpired_WhenInvitationIsExpired()
    {
        var creator = await CreateUserAsync();
        var acceptor = await CreateUserAsync();
        var account = await CreateAccountAsync();

        var invitation = await CreateInvitationEntityAsync(
            account.Id,
            creator.Id,
            expiresUtc: DateTime.UtcNow.AddHours(-1)
        );

        SetUserContext(acceptor);

        var result = await _invitationProcessor.AcceptInvitationAsync(
            new AcceptInvitationRequest(invitation.Token)
        );

        Assert.Equal(AcceptInvitationResultCode.InvitationExpiredError, result.ResultCode);
    }

    [Fact]
    public async Task AcceptInvitationAsync_ReturnsRevoked_WhenInvitationIsRevoked()
    {
        var creator = await CreateUserAsync();
        var acceptor = await CreateUserAsync();
        var account = await CreateAccountAsync();

        var invitation = await CreateInvitationEntityAsync(
            account.Id,
            creator.Id,
            revokedUtc: DateTime.UtcNow.AddMinutes(-5)
        );

        SetUserContext(acceptor);

        var result = await _invitationProcessor.AcceptInvitationAsync(
            new AcceptInvitationRequest(invitation.Token)
        );

        Assert.Equal(AcceptInvitationResultCode.InvitationRevokedError, result.ResultCode);
    }

    [Fact]
    public async Task AcceptInvitationAsync_ReturnsAlreadyAccepted_WhenInvitationWasAccepted()
    {
        var creator = await CreateUserAsync();
        var acceptor = await CreateUserAsync();
        var account = await CreateAccountAsync();

        var invitation = await CreateInvitationEntityAsync(
            account.Id,
            creator.Id,
            acceptedByUserId: Guid.CreateVersion7(),
            acceptedUtc: DateTime.UtcNow.AddMinutes(-5)
        );

        SetUserContext(acceptor);

        var result = await _invitationProcessor.AcceptInvitationAsync(
            new AcceptInvitationRequest(invitation.Token)
        );

        Assert.Equal(AcceptInvitationResultCode.InvitationAlreadyAcceptedError, result.ResultCode);
    }

    // ─────────────────────────────────────────────
    // RevokeInvitationAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task RevokeInvitationAsync_ReturnsSuccess_WhenInvitationExists()
    {
        var creator = await CreateUserAsync();
        var account = await CreateAccountAsync();

        var invitation = await CreateInvitationEntityAsync(account.Id, creator.Id);

        var result = await _invitationProcessor.RevokeInvitationAsync(invitation.Id);

        Assert.Equal(RevokeInvitationResultCode.Success, result.ResultCode);

        // Verify the invitation entity has RevokedUtc set
        var repo = _serviceFactory.GetRequiredService<IRepo<InvitationEntity>>();
        var entity = await repo.GetAsync(i => i.Id == invitation.Id);
        Assert.NotNull(entity);
        Assert.NotNull(entity.RevokedUtc);
    }

    [Fact]
    public async Task RevokeInvitationAsync_ReturnsNotFound_WhenInvitationDoesNotExist()
    {
        var result = await _invitationProcessor.RevokeInvitationAsync(Guid.CreateVersion7());

        Assert.Equal(RevokeInvitationResultCode.InvitationNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task RevokeInvitationAsync_ReturnsAlreadyAccepted_WhenInvitationWasAccepted()
    {
        var creator = await CreateUserAsync();
        var account = await CreateAccountAsync();

        var invitation = await CreateInvitationEntityAsync(
            account.Id,
            creator.Id,
            acceptedByUserId: Guid.CreateVersion7(),
            acceptedUtc: DateTime.UtcNow.AddMinutes(-5)
        );

        var result = await _invitationProcessor.RevokeInvitationAsync(invitation.Id);

        Assert.Equal(RevokeInvitationResultCode.InvitationAlreadyAcceptedError, result.ResultCode);
    }

    // ─────────────────────────────────────────────
    // ListInvitationsAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task ListInvitationsAsync_ReturnsInvitations_ForAccount()
    {
        var creator = await CreateUserAsync();
        var account = await CreateAccountAsync();
        SetUserContext(creator, account);

        await _invitationProcessor.CreateInvitationAsync(
            new CreateInvitationRequest(
                account.Id,
                "one@test.com",
                null,
                InvitationConstants.MIN_EXPIRY_SECONDS
            )
        );
        await _invitationProcessor.CreateInvitationAsync(
            new CreateInvitationRequest(
                account.Id,
                "two@test.com",
                null,
                InvitationConstants.MIN_EXPIRY_SECONDS
            )
        );

        var result = await _invitationProcessor.ListInvitationsAsync(account.Id, null, null, 0, 10);

        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Items.Count);
    }
}
