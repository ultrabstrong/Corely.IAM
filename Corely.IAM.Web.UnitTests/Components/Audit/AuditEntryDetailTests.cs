using Corely.IAM.Audits.Models;
using Corely.IAM.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Services;
using Corely.IAM.Web.Components;
using Corely.IAM.Web.Components.Audit;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TestContext = Bunit.TestContext;

namespace Corely.IAM.Web.UnitTests.Components.Audit;

public class AuditEntryDetailTests : TestContext
{
    private readonly Mock<IAuditService> _auditService = new();
    private readonly AuditEntry _entry = new(
        Guid.CreateVersion7(),
        new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
        Guid.CreateVersion7(),
        AuditCohort.AccountMember,
        Guid.CreateVersion7(),
        "tests",
        "IRegistrationService",
        "RegisterUsersWithGroupAsync",
        AuthAction.Update,
        "group",
        [.. Enumerable.Range(0, 5).Select(_ => Guid.CreateVersion7())],
        "Success",
        null
    );

    public AuditEntryDetailTests()
    {
        var userContextAccessor = new Mock<IBlazorUserContextAccessor>();
        userContextAccessor
            .Setup(x => x.GetUserContextAsync())
            .ReturnsAsync(PageTestHelpers.CreateUserContext());
        Services.AddSingleton<IIamWebLinks>(new FakeIamWebLinks());
        Services.AddSingleton(userContextAccessor.Object);
        Services.AddSingleton(_auditService.Object);
        Services.AddSingleton<ILogger<EntityPageBase>>(NullLogger<EntityPageBase>.Instance);
    }

    [Fact]
    public void Page_ShowsThePlainActivity_EveryResourceId_AndTheTechnicalCall()
    {
        _auditService
            .Setup(x => x.GetEntryAsync(_entry.Id))
            .ReturnsAsync(
                new RetrieveSingleResult<AuditEntry>(RetrieveResultCode.Success, "", _entry, null)
            );

        var cut = Render<AuditEntryDetail>(p => p.Add(x => x.Id, _entry.Id));

        cut.WaitForAssertion(() => Assert.Contains("Register users with group", cut.Markup));
        foreach (var id in _entry.ResourceIds)
        {
            Assert.Contains(id.ToString(), cut.Markup);
        }
        Assert.Contains("IRegistrationService.RegisterUsersWithGroupAsync", cut.Markup);
    }

    [Fact]
    public void Page_SaysNotFound_WhenTheEntryIsNotVisible()
    {
        _auditService
            .Setup(x => x.GetEntryAsync(_entry.Id))
            .ReturnsAsync(
                new RetrieveSingleResult<AuditEntry>(
                    RetrieveResultCode.NotFoundError,
                    "",
                    null,
                    null
                )
            );

        var cut = Render<AuditEntryDetail>(p => p.Add(x => x.Id, _entry.Id));

        cut.WaitForAssertion(() => Assert.Contains("Audit entry not found.", cut.Markup));
    }
}
