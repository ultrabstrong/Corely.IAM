using Corely.IAM.Audits.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;

namespace Corely.IAM.UnitTests.Audits.Decorators;

public class AuditDecoratorMappingTests
{
    private readonly CapturingAuditProvider _auditProvider = new();
    private readonly Guid _accountId = Guid.CreateVersion7();

    [Fact]
    public async Task RegisterUser_RecordsACreatedUser_OutsideAnyAccount_AsItsOwnActor()
    {
        var userId = Guid.CreateVersion7();
        var inner = new Mock<IRegistrationService>();
        inner
            .Setup(s => s.RegisterUserAsync(It.IsAny<RegisterUserRequest>()))
            .ReturnsAsync(new RegisterUserResult(RegisterUserResultCode.Success, "", userId));

        await new RegistrationServiceAuditDecorator(inner.Object, _auditProvider).RegisterUserAsync(
            new RegisterUserRequest("user", "user@example.com", "pw")
        );

        var captured = Assert.Single(_auditProvider.Calls);
        Assert.Equal(AuthAction.Create, captured.Call.Action);
        Assert.Equal(PermissionConstants.USER_RESOURCE_TYPE, captured.Call.ResourceType);
        Assert.Equal(Guid.Empty, captured.Call.AccountId);
        Assert.Equal([userId], captured.Outcome!.ResourceIds);
        Assert.Equal(userId, captured.Outcome.ActorUserId);
    }

    [Fact]
    public async Task RegisterAccount_RecordsTheCreation_InTheNewAccount()
    {
        var inner = new Mock<IRegistrationService>();
        inner
            .Setup(s => s.RegisterAccountAsync(It.IsAny<RegisterAccountRequest>()))
            .ReturnsAsync(
                new RegisterAccountResult(RegisterAccountResultCode.Success, "", _accountId)
            );

        await new RegistrationServiceAuditDecorator(
            inner.Object,
            _auditProvider
        ).RegisterAccountAsync(new RegisterAccountRequest("Acme", Guid.CreateVersion7()));

        var captured = Assert.Single(_auditProvider.Calls);
        Assert.Equal(PermissionConstants.ACCOUNT_RESOURCE_TYPE, captured.Call.ResourceType);
        Assert.Equal(_accountId, captured.Outcome!.AccountId);
        Assert.Equal([_accountId], captured.Outcome.ResourceIds);
    }

    [Fact]
    public async Task RegisterUsersWithGroup_RecordsAnUpdateOfTheGroup_WithTheUsersAdded()
    {
        var groupId = Guid.CreateVersion7();
        var userIds = new List<Guid> { Guid.CreateVersion7(), Guid.CreateVersion7() };
        var inner = new Mock<IRegistrationService>();
        inner
            .Setup(s => s.RegisterUsersWithGroupAsync(It.IsAny<RegisterUsersWithGroupRequest>()))
            .ReturnsAsync(
                new RegisterUsersWithGroupResult(AddUsersToGroupResultCode.Success, "", 2)
            );

        await new RegistrationServiceAuditDecorator(
            inner.Object,
            _auditProvider
        ).RegisterUsersWithGroupAsync(
            new RegisterUsersWithGroupRequest(userIds, groupId, _accountId)
        );

        var captured = Assert.Single(_auditProvider.Calls);
        Assert.Equal(AuthAction.Update, captured.Call.Action);
        Assert.Equal(PermissionConstants.GROUP_RESOURCE_TYPE, captured.Call.ResourceType);
        Assert.Equal(_accountId, captured.Call.AccountId);
        Assert.Equal([groupId, .. userIds], captured.Call.ResourceIds);
    }

    [Fact]
    public async Task DeregisterUser_AsksForTheDeletedUsername()
    {
        var inner = new Mock<IDeregistrationService>();
        inner
            .Setup(s => s.DeregisterUserAsync())
            .ReturnsAsync(new DeregisterUserResult(DeregisterUserResultCode.Success, ""));

        await new DeregistrationServiceAuditDecorator(
            inner.Object,
            _auditProvider
        ).DeregisterUserAsync();

        var captured = Assert.Single(_auditProvider.Calls);
        Assert.Equal(AuthAction.Delete, captured.Call.Action);
        Assert.Equal(AuditDetail.DeletedUsername, captured.Call.Detail);
        Assert.Equal(Guid.Empty, captured.Call.AccountId);
    }

    [Fact]
    public async Task DeregisterAccount_AsksForTheDeletedAccountName_InThatAccount()
    {
        var inner = new Mock<IDeregistrationService>();
        inner
            .Setup(s => s.DeregisterAccountAsync(It.IsAny<DeregisterAccountRequest>()))
            .ReturnsAsync(new DeregisterAccountResult(DeregisterAccountResultCode.Success, ""));

        await new DeregistrationServiceAuditDecorator(
            inner.Object,
            _auditProvider
        ).DeregisterAccountAsync(new DeregisterAccountRequest(_accountId));

        var captured = Assert.Single(_auditProvider.Calls);
        Assert.Equal(AuditDetail.DeletedAccountName, captured.Call.Detail);
        Assert.Equal(_accountId, captured.Call.AccountId);
        Assert.Equal([_accountId], captured.Call.ResourceIds);
    }

    [Fact]
    public async Task SignIn_PassesTheUsername_SoAFailedSignInNamesTheActor()
    {
        var inner = new Mock<IAuthenticationService>();
        inner
            .Setup(s => s.SignInAsync(It.IsAny<SignInRequest>()))
            .ReturnsAsync(new SignInResult(SignInResultCode.PasswordMismatchError, "", null, null));

        await new AuthenticationServiceAuditDecorator(inner.Object, _auditProvider).SignInAsync(
            new SignInRequest("someone", "wrong", "device")
        );

        var captured = Assert.Single(_auditProvider.Calls);
        Assert.Equal(AuthAction.Execute, captured.Call.Action);
        Assert.Equal("someone", captured.Call.ActorUsername);
        Assert.Equal(Guid.Empty, captured.Call.AccountId);
        Assert.Equal(nameof(SignInResultCode.PasswordMismatchError), captured.Outcome!.ResultCode);
    }

    [Fact]
    public async Task SignIn_IntoAnAccount_RecordsThatAccount()
    {
        var inner = new Mock<IAuthenticationService>();
        inner
            .Setup(s => s.SignInAsync(It.IsAny<SignInRequest>()))
            .ReturnsAsync(new SignInResult(SignInResultCode.Success, null, "token", Guid.Empty));

        await new AuthenticationServiceAuditDecorator(inner.Object, _auditProvider).SignInAsync(
            new SignInRequest("someone", "pw", "device", _accountId)
        );

        Assert.Equal(_accountId, Assert.Single(_auditProvider.Calls).Call.AccountId);
    }

    [Fact]
    public async Task SwitchAccount_RecordsEnteringTheAccount()
    {
        var inner = new Mock<IAuthenticationService>();
        inner
            .Setup(s => s.SwitchAccountAsync(It.IsAny<SwitchAccountRequest>()))
            .ReturnsAsync(new SignInResult(SignInResultCode.Success, null, "token", Guid.Empty));

        await new AuthenticationServiceAuditDecorator(
            inner.Object,
            _auditProvider
        ).SwitchAccountAsync(new SwitchAccountRequest(_accountId));

        var captured = Assert.Single(_auditProvider.Calls);
        Assert.Equal(AuthAction.Execute, captured.Call.Action);
        Assert.Equal(PermissionConstants.ACCOUNT_RESOURCE_TYPE, captured.Call.ResourceType);
        Assert.Equal(_accountId, captured.Call.AccountId);
    }

    [Theory]
    [InlineData(true, "Success")]
    [InlineData(false, "Failed")]
    public async Task SignOut_RecordsWhetherItSucceeded(bool signedOut, string expected)
    {
        var inner = new Mock<IAuthenticationService>();
        inner.Setup(s => s.SignOutAsync(It.IsAny<SignOutRequest>())).ReturnsAsync(signedOut);

        await new AuthenticationServiceAuditDecorator(inner.Object, _auditProvider).SignOutAsync(
            new SignOutRequest("token")
        );

        Assert.Equal(expected, Assert.Single(_auditProvider.Calls).Outcome?.ResultCode);
    }

    [Fact]
    public async Task AcceptInvitation_RecordsTheJoinedAccount_FromTheResult()
    {
        var inner = new Mock<IInvitationService>();
        inner
            .Setup(s => s.AcceptInvitationAsync(It.IsAny<AcceptInvitationRequest>()))
            .ReturnsAsync(
                new AcceptInvitationResult(AcceptInvitationResultCode.Success, "", _accountId)
            );

        await new InvitationServiceAuditDecorator(
            inner.Object,
            _auditProvider
        ).AcceptInvitationAsync(new AcceptInvitationRequest("token"));

        Assert.Equal(_accountId, Assert.Single(_auditProvider.Calls).Outcome!.AccountId);
    }

    [Fact]
    public async Task ListUsers_RecordsARead_InTheRequestedAccount()
    {
        var inner = new Mock<IRetrievalService>();
        inner
            .Setup(s => s.ListUsersAsync(It.IsAny<ListUsersRequest>()))
            .ReturnsAsync(new RetrieveListResult<User>(RetrieveResultCode.Success, "", null));

        await new RetrievalServiceAuditDecorator(inner.Object, _auditProvider).ListUsersAsync(
            new ListUsersRequest(_accountId)
        );

        var captured = Assert.Single(_auditProvider.Calls);
        Assert.Equal(AuthAction.Read, captured.Call.Action);
        Assert.Equal(_accountId, captured.Call.AccountId);
    }

    [Fact]
    public async Task GetUser_RecordsARead_InTheCurrentAccount()
    {
        var userId = Guid.CreateVersion7();
        var inner = new Mock<IRetrievalService>();
        inner
            .Setup(s => s.GetUserAsync(userId, false))
            .ReturnsAsync(
                new RetrieveSingleResult<User>(RetrieveResultCode.Success, "", null, null)
            );

        await new RetrievalServiceAuditDecorator(inner.Object, _auditProvider).GetUserAsync(userId);

        var captured = Assert.Single(_auditProvider.Calls);
        Assert.Null(captured.Call.AccountId);
        Assert.Equal([userId], captured.Call.ResourceIds);
    }
}
