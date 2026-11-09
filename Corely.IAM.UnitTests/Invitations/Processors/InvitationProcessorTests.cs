using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Invitations.Entities;
using Corely.IAM.Invitations.Enums;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Invitations.Processors;
using Corely.IAM.Invitations.Providers;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using Corely.Security.Hashing.Factories;
using Corely.Security.Secrets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Invitations.Processors;

public class InvitationProcessorTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly InvitationProcessor _invitationProcessor;

    public InvitationProcessorTests()
    {
        _invitationProcessor = CreateProcessor();
    }

    private InvitationProcessor CreateProcessor(IInvitationSender? sender = null) =>
        new(
            _serviceFactory.GetRequiredService<IRepo<InvitationEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<AccountEntity>>(),
            _serviceFactory.GetRequiredService<IAccountProcessor>(),
            _serviceFactory.GetRequiredService<IUserContextProvider>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<IHashProviderFactory>(),
            _serviceFactory.GetRequiredService<IamHashCodes>(),
            _serviceFactory.GetRequiredService<ISecretProvider>(),
            _serviceFactory.GetRequiredService<TimeProvider>(),
            _serviceFactory.GetRequiredService<ILogger<InvitationProcessor>>(),
            sender
        );

    private async Task<UserEntity> CreateUserAsync(string? email = null)
    {
        var user = new UserEntity
        {
            Id = Guid.CreateVersion7(),
            Username = "testuser",
            Email = email ?? "default@test.com",
            Accounts = [],
            Groups = [],
            Roles = [],
        };
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        return await userRepo.CreateAsync(user);
    }

    private async Task<UserEntity> CreateUserInAccountAsync(AccountEntity account, string email)
    {
        var user = new UserEntity
        {
            Id = Guid.CreateVersion7(),
            Username = "member",
            Email = email,
            Accounts = [account],
            Groups = [],
            Roles = [],
        };
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var created = await userRepo.CreateAsync(user);

        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var accountEntity = await accountRepo.GetAsync(
            a => a.Id == account.Id,
            include: q => q.Include(a => a.Users)
        );
        accountEntity!.Users ??= [];
        accountEntity.Users.Add(created);
        await accountRepo.UpdateAsync(accountEntity);

        return created;
    }

    private async Task<AccountEntity> CreateAccountAsync(string accountName = "testaccount")
    {
        var account = new AccountEntity
        {
            Id = Guid.CreateVersion7(),
            AccountName = accountName,
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
            Username = userEntity.Username,
            Email = userEntity.Email,
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

    private async Task<(InvitationEntity Entity, string Token)> CreateInvitationEntityAsync(
        Guid accountId,
        Guid createdByUserId,
        string email = "invite@test.com",
        DateTime? expiresUtc = null,
        Guid? acceptedByUserId = null,
        DateTime? acceptedUtc = null,
        DateTime? revokedUtc = null
    )
    {
        var secret = _serviceFactory.GetRequiredService<ISecretProvider>().CreateSecret();
        var entity = new InvitationEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            SecretHash = secret
                .ToHashedValueFromPlainText(
                    _serviceFactory.GetRequiredService<IHashProviderFactory>(),
                    _serviceFactory.GetRequiredService<IamHashCodes>().TokenHashCode
                )
                .ToHashString()!,
            CreatedByUserId = createdByUserId,
            Email = email,
            ExpiresUtc = expiresUtc ?? DateTime.UtcNow.AddDays(7),
            AcceptedByUserId = acceptedByUserId,
            AcceptedUtc = acceptedUtc,
            RevokedUtc = revokedUtc,
        };
        var repo = _serviceFactory.GetRequiredService<IRepo<InvitationEntity>>();
        return (await repo.CreateAsync(entity), new InvitationToken(entity.Id, secret).ToString());
    }

    [Fact]
    public async Task CreateInvitation_ReturnsSuccess_WithValidRequest()
    {
        var user = await CreateUserAsync();
        var account = await CreateAccountAsync();
        SetUserContext(user, account);

        var request = new CreateInvitationRequest(
            account.Id,
            "invite@example.com",
            "Join us",
            CreateInvitationRequest.MIN_EXPIRY_SECONDS
        );

        var result = await _invitationProcessor.CreateInvitationAsync(request);

        Assert.Equal(CreateInvitationResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.InvitationId);
        Assert.NotEqual(Guid.Empty, result.InvitationId!.Value);
    }

    [Fact]
    public async Task CreateInvitation_ReturnsAccountNotFound_WhenAccountDoesNotExist()
    {
        var user = await CreateUserAsync();
        SetUserContext(user);

        var request = new CreateInvitationRequest(
            Guid.CreateVersion7(),
            "invite@example.com",
            null,
            CreateInvitationRequest.MIN_EXPIRY_SECONDS
        );

        var result = await _invitationProcessor.CreateInvitationAsync(request);

        Assert.Equal(CreateInvitationResultCode.AccountNotFoundError, result.ResultCode);
        Assert.Null(result.Token);
        Assert.Null(result.InvitationId);
    }

    [Fact]
    public async Task CreateInvitationAsync_ReturnsUserAlreadyInAccountError_WhenEmailBelongsToExistingMember()
    {
        var owner = await CreateUserAsync();
        var account = await CreateAccountAsync();
        SetUserContext(owner, account);

        const string memberEmail = "already-member@example.com";
        await CreateUserInAccountAsync(account, memberEmail);

        var request = new CreateInvitationRequest(
            account.Id,
            memberEmail,
            null,
            CreateInvitationRequest.MIN_EXPIRY_SECONDS
        );

        var result = await _invitationProcessor.CreateInvitationAsync(request);

        Assert.Equal(CreateInvitationResultCode.UserAlreadyInAccountError, result.ResultCode);
        Assert.NotNull(result.ExistingUserId);
        Assert.NotEqual(Guid.Empty, result.ExistingUserId!.Value);
        Assert.Null(result.Token);
        Assert.Null(result.InvitationId);
    }

    [Fact]
    public async Task AcceptInvitation_ReturnsSuccess_AndAddsUserToAccount()
    {
        var creator = await CreateUserAsync();
        var acceptor = await CreateUserAsync("acceptor@test.com");
        var account = await CreateAccountAsync();
        SetUserContext(creator, account);

        var createResult = await _invitationProcessor.CreateInvitationAsync(
            new CreateInvitationRequest(
                account.Id,
                "acceptor@test.com",
                null,
                CreateInvitationRequest.MIN_EXPIRY_SECONDS
            )
        );

        SetUserContext(acceptor);

        var result = await _invitationProcessor.AcceptInvitationAsync(
            new AcceptInvitationRequest(createResult.Token!)
        );

        Assert.Equal(AcceptInvitationResultCode.Success, result.ResultCode);
        Assert.Equal(account.Id, result.AccountId);

        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var accountEntity = await accountRepo.GetAsync(
            a => a.Id == account.Id,
            include: q => q.Include(a => a.Users)
        );
        Assert.NotNull(accountEntity?.Users);
        Assert.Contains(accountEntity.Users, u => u.Id == acceptor.Id);
    }

    [Fact]
    public async Task AcceptInvitation_ReturnsSuccess_WhenUserAlreadyInAccount()
    {
        var creator = await CreateUserAsync();
        var user = await CreateUserAsync("user@test.com");
        var account = await CreateAccountAsync();

        var invitation = await CreateInvitationEntityAsync(
            account.Id,
            creator.Id,
            email: "user@test.com"
        );

        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var accountEntity = await accountRepo.GetAsync(
            a => a.Id == account.Id,
            include: q => q.Include(a => a.Users)
        );
        accountEntity!.Users ??= [];
        accountEntity.Users.Add(user);
        await accountRepo.UpdateAsync(accountEntity);

        SetUserContext(user, account);

        var result = await _invitationProcessor.AcceptInvitationAsync(
            new AcceptInvitationRequest(invitation.Token)
        );

        Assert.Equal(AcceptInvitationResultCode.Success, result.ResultCode);
        Assert.Equal(account.Id, result.AccountId);
    }

    [Fact]
    public async Task AcceptInvitation_BurnsSiblingInvitations_ForSameAccountAndEmail()
    {
        var creator = await CreateUserAsync();
        var acceptor = await CreateUserAsync("sibling@test.com");
        var account = await CreateAccountAsync();
        SetUserContext(creator, account);

        var email = "sibling@test.com";

        var result1 = await _invitationProcessor.CreateInvitationAsync(
            new CreateInvitationRequest(
                account.Id,
                email,
                "First",
                CreateInvitationRequest.MIN_EXPIRY_SECONDS
            )
        );
        var result2 = await _invitationProcessor.CreateInvitationAsync(
            new CreateInvitationRequest(
                account.Id,
                email,
                "Second",
                CreateInvitationRequest.MIN_EXPIRY_SECONDS
            )
        );

        SetUserContext(acceptor);

        var acceptResult = await _invitationProcessor.AcceptInvitationAsync(
            new AcceptInvitationRequest(result1.Token!)
        );
        Assert.Equal(AcceptInvitationResultCode.Success, acceptResult.ResultCode);

        var invitationRepo = _serviceFactory.GetRequiredService<IRepo<InvitationEntity>>();
        var siblingEntity = await invitationRepo.GetAsync(i => i.Id == result2.InvitationId);
        Assert.NotNull(siblingEntity);
        Assert.NotNull(siblingEntity.AcceptedByUserId);
        Assert.Equal(acceptor.Id, siblingEntity.AcceptedByUserId);
    }

    [Fact]
    public async Task AcceptInvitation_ReturnsEmailMismatchError_WhenUserEmailDoesNotMatch()
    {
        var creator = await CreateUserAsync();
        var acceptor = await CreateUserAsync("wrong@test.com");
        var account = await CreateAccountAsync();
        SetUserContext(creator, account);

        var createResult = await _invitationProcessor.CreateInvitationAsync(
            new CreateInvitationRequest(
                account.Id,
                "intended@test.com",
                null,
                CreateInvitationRequest.MIN_EXPIRY_SECONDS
            )
        );

        SetUserContext(acceptor);

        var result = await _invitationProcessor.AcceptInvitationAsync(
            new AcceptInvitationRequest(createResult.Token!)
        );

        Assert.Equal(AcceptInvitationResultCode.EmailMismatchError, result.ResultCode);
        Assert.Null(result.AccountId);

        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var accountEntity = await accountRepo.GetAsync(
            a => a.Id == account.Id,
            include: q => q.Include(a => a.Users)
        );
        Assert.DoesNotContain(accountEntity!.Users!, u => u.Id == acceptor.Id);
    }

    [Fact]
    public async Task AcceptInvitation_Succeeds_WhenUserEmailMatchesCaseInsensitive()
    {
        var creator = await CreateUserAsync();
        var acceptor = await CreateUserAsync("Alice@Example.COM");
        var account = await CreateAccountAsync();
        SetUserContext(creator, account);

        var createResult = await _invitationProcessor.CreateInvitationAsync(
            new CreateInvitationRequest(
                account.Id,
                "alice@example.com",
                null,
                CreateInvitationRequest.MIN_EXPIRY_SECONDS
            )
        );

        SetUserContext(acceptor);

        var result = await _invitationProcessor.AcceptInvitationAsync(
            new AcceptInvitationRequest(createResult.Token!)
        );

        Assert.Equal(AcceptInvitationResultCode.Success, result.ResultCode);
        Assert.Equal(account.Id, result.AccountId);
    }

    [Fact]
    public async Task AcceptInvitation_ReturnsNotFound_WhenTokenIsEmpty()
    {
        var result = await _invitationProcessor.AcceptInvitationAsync(
            new AcceptInvitationRequest("")
        );

        Assert.Equal(AcceptInvitationResultCode.InvitationNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task AcceptInvitation_ReturnsNotFound_WhenTokenDoesNotExist()
    {
        var result = await _invitationProcessor.AcceptInvitationAsync(
            new AcceptInvitationRequest("nonexistent-token")
        );

        Assert.Equal(AcceptInvitationResultCode.InvitationNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task AcceptInvitation_ReturnsExpired_WhenInvitationIsExpired()
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
    public async Task AcceptInvitation_ReturnsRevoked_WhenInvitationIsRevoked()
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
    public async Task AcceptInvitation_ReturnsAlreadyAccepted_WhenInvitationWasAccepted()
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

    [Fact]
    public async Task RevokeInvitation_ReturnsSuccess_WhenInvitationExists()
    {
        var creator = await CreateUserAsync();
        var account = await CreateAccountAsync();

        var invitation = await CreateInvitationEntityAsync(account.Id, creator.Id);

        var result = await _invitationProcessor.RevokeInvitationAsync(
            new RevokeInvitationRequest(account.Id, invitation.Entity.Id)
        );

        Assert.Equal(RevokeInvitationResultCode.Success, result.ResultCode);

        var repo = _serviceFactory.GetRequiredService<IRepo<InvitationEntity>>();
        var entity = await repo.GetAsync(i => i.Id == invitation.Entity.Id);
        Assert.NotNull(entity);
        Assert.NotNull(entity.RevokedUtc);
    }

    [Fact]
    public async Task RevokeInvitation_ReturnsNotFound_WhenInvitationDoesNotExist()
    {
        var result = await _invitationProcessor.RevokeInvitationAsync(
            new RevokeInvitationRequest(Guid.CreateVersion7(), Guid.CreateVersion7())
        );

        Assert.Equal(RevokeInvitationResultCode.InvitationNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task RevokeInvitation_ReturnsAlreadyAccepted_WhenInvitationWasAccepted()
    {
        var creator = await CreateUserAsync();
        var account = await CreateAccountAsync();

        var invitation = await CreateInvitationEntityAsync(
            account.Id,
            creator.Id,
            acceptedByUserId: Guid.CreateVersion7(),
            acceptedUtc: DateTime.UtcNow.AddMinutes(-5)
        );

        var result = await _invitationProcessor.RevokeInvitationAsync(
            new RevokeInvitationRequest(account.Id, invitation.Entity.Id)
        );

        Assert.Equal(RevokeInvitationResultCode.InvitationAlreadyAcceptedError, result.ResultCode);
    }

    [Fact]
    public async Task RevokeInvitation_ReturnsNotFound_WhenInvitationBelongsToDifferentAccount()
    {
        var creator = await CreateUserAsync();
        var account = await CreateAccountAsync();
        var otherAccount = await CreateAccountAsync("OtherAccount");

        var invitation = await CreateInvitationEntityAsync(otherAccount.Id, creator.Id);

        var result = await _invitationProcessor.RevokeInvitationAsync(
            new RevokeInvitationRequest(account.Id, invitation.Entity.Id)
        );

        Assert.Equal(RevokeInvitationResultCode.InvitationNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task ListInvitations_ReturnsInvitations_ForAccount()
    {
        var creator = await CreateUserAsync();
        var account = await CreateAccountAsync();
        SetUserContext(creator, account);

        await _invitationProcessor.CreateInvitationAsync(
            new CreateInvitationRequest(
                account.Id,
                "one@test.com",
                null,
                CreateInvitationRequest.MIN_EXPIRY_SECONDS
            )
        );
        await _invitationProcessor.CreateInvitationAsync(
            new CreateInvitationRequest(
                account.Id,
                "two@test.com",
                null,
                CreateInvitationRequest.MIN_EXPIRY_SECONDS
            )
        );

        var result = await _invitationProcessor.ListInvitationsAsync(
            new ListInvitationsRequest(account.Id, Skip: 0, Take: 10)
        );

        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Items.Count);
    }

    private async Task<(
        AccountEntity Account,
        CreateInvitationResult Result
    )> CreateWithSenderAsync(IInvitationSender? sender)
    {
        var user = await CreateUserAsync("inviter@test.com");
        var account = await CreateAccountAsync("Acme");
        SetUserContext(user, account);
        var result = await CreateProcessor(sender)
            .CreateInvitationAsync(
                new CreateInvitationRequest(
                    account.Id,
                    "invitee@test.com",
                    "Welcome",
                    CreateInvitationRequest.MIN_EXPIRY_SECONDS
                )
            );
        return (account, result);
    }

    [Fact]
    public async Task CreateInvitation_StoresOnlyTheSecretsHash_ForNewInvitation()
    {
        var (_, result) = await CreateWithSenderAsync(null);

        var entity = await _serviceFactory
            .GetRequiredService<IRepo<InvitationEntity>>()
            .GetAsync(i => i.Id == result.InvitationId);
        Assert.True(InvitationToken.TryParse(result.Token, out var token));
        Assert.Equal(result.InvitationId, token.InvitationId);
        Assert.DoesNotContain(token.Secret, entity!.SecretHash);
    }

    [Fact]
    public async Task CreateInvitation_ReturnsTokenNotAttempted_ForNoSender()
    {
        var (_, result) = await CreateWithSenderAsync(null);

        Assert.Equal(InvitationDeliveryStatus.NotAttempted, result.Delivery);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.ExpiresUtc);
        Assert.Null(result.DeliveryMessage);
    }

    [Fact]
    public async Task CreateInvitation_HandsTheSenderEveryFact_ForRegisteredSender()
    {
        InvitationDelivery? delivered = null;
        var sender = new Mock<IInvitationSender>();
        sender
            .Setup(s => s.SendAsync(It.IsAny<InvitationDelivery>()))
            .Callback<InvitationDelivery>(d => delivered = d)
            .ReturnsAsync(new InvitationSendResult(InvitationSendResultCode.Sent));

        var (account, result) = await CreateWithSenderAsync(sender.Object);

        Assert.NotNull(delivered);
        Assert.Equal(result.InvitationId, delivered.InvitationId);
        Assert.Equal(account.Id, delivered.AccountId);
        Assert.Equal("Acme", delivered.AccountName);
        Assert.Equal("invitee@test.com", delivered.Email);
        Assert.Equal("Welcome", delivered.Description);
        Assert.Equal(result.ExpiresUtc, delivered.ExpiresUtc);
        Assert.Equal("inviter@test.com", delivered.InvitedByEmail);

        SetUserContext(await CreateUserAsync("invitee@test.com"));
        var accepted = await _invitationProcessor.AcceptInvitationAsync(
            new AcceptInvitationRequest(delivered.Token)
        );
        Assert.Equal(AcceptInvitationResultCode.Success, accepted.ResultCode);
    }

    [Fact]
    public async Task CreateInvitation_WithholdsTokenWithDefaultMessage_ForSentWithoutMessage()
    {
        var sender = new Mock<IInvitationSender>();
        sender
            .Setup(s => s.SendAsync(It.IsAny<InvitationDelivery>()))
            .ReturnsAsync(new InvitationSendResult(InvitationSendResultCode.Sent));

        var (_, result) = await CreateWithSenderAsync(sender.Object);

        Assert.Equal(InvitationDeliveryStatus.Sent, result.Delivery);
        Assert.Null(result.Token);
        Assert.Equal("Invitation sent to invitee@test.com.", result.DeliveryMessage);
    }

    [Fact]
    public async Task CreateInvitation_ReturnsSendersMessage_ForSentWithMessage()
    {
        var sender = new Mock<IInvitationSender>();
        sender
            .Setup(s => s.SendAsync(It.IsAny<InvitationDelivery>()))
            .ReturnsAsync(
                new InvitationSendResult(InvitationSendResultCode.Sent, "Emailed, check spam.")
            );

        var (_, result) = await CreateWithSenderAsync(sender.Object);

        Assert.Equal("Emailed, check spam.", result.DeliveryMessage);
    }

    [Fact]
    public async Task CreateInvitation_ReturnsTokenAndReason_ForNotSent()
    {
        var sender = new Mock<IInvitationSender>();
        sender
            .Setup(s => s.SendAsync(It.IsAny<InvitationDelivery>()))
            .ReturnsAsync(
                new InvitationSendResult(InvitationSendResultCode.NotSent, "mailbox full")
            );

        var (_, result) = await CreateWithSenderAsync(sender.Object);

        Assert.Equal(CreateInvitationResultCode.Success, result.ResultCode);
        Assert.Equal(InvitationDeliveryStatus.Failed, result.Delivery);
        Assert.NotNull(result.Token);
        Assert.Equal("mailbox full", result.DeliveryMessage);
    }

    [Fact]
    public async Task CreateInvitation_ReturnsToken_ForSenderThatThrows()
    {
        var sender = new Mock<IInvitationSender>();
        sender
            .Setup(s => s.SendAsync(It.IsAny<InvitationDelivery>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var (_, result) = await CreateWithSenderAsync(sender.Object);

        Assert.Equal(CreateInvitationResultCode.Success, result.ResultCode);
        Assert.Equal(InvitationDeliveryStatus.Failed, result.Delivery);
        Assert.NotNull(result.Token);
    }

    [Fact]
    public async Task AcceptInvitation_ReturnsNotFound_ForWrongSecret()
    {
        var creator = await CreateUserAsync();
        var acceptor = await CreateUserAsync("invite@test.com");
        var account = await CreateAccountAsync();
        var (entity, _) = await CreateInvitationEntityAsync(account.Id, creator.Id);
        SetUserContext(acceptor);

        var result = await _invitationProcessor.AcceptInvitationAsync(
            new AcceptInvitationRequest(new InvitationToken(entity.Id, "not-the-secret").ToString())
        );

        Assert.Equal(AcceptInvitationResultCode.InvitationNotFoundError, result.ResultCode);
    }
}
