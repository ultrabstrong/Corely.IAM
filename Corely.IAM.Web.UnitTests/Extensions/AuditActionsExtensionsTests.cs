using Corely.IAM.Audits.Models;
using Corely.IAM.Security.Constants;
using Corely.IAM.Web.Extensions;

namespace Corely.IAM.Web.UnitTests.Extensions;

public class AuditActionsExtensionsTests
{
    [Theory]
    [InlineData(AuditActions.Create | AuditActions.Read, AuthAction.Read, true)]
    [InlineData(AuditActions.Create | AuditActions.Read, AuthAction.Delete, false)]
    [InlineData(AuditActions.None, AuthAction.Create, false)]
    public void Has_ReadsTheActionsFlag(AuditActions actions, AuthAction action, bool expected) =>
        Assert.Equal(expected, actions.Has(action));

    [Fact]
    public void With_AddsAndRemovesOneAction()
    {
        var actions = AuditActions.Create;

        var added = actions.With(AuthAction.Execute, true);
        var removed = added.With(AuthAction.Create, false);

        Assert.Equal(AuditActions.Create | AuditActions.Execute, added);
        Assert.Equal(AuditActions.Execute, removed);
    }

    [Theory]
    [InlineData(AuthAction.Create, AuditActions.Create)]
    [InlineData(AuthAction.Read, AuditActions.Read)]
    [InlineData(AuthAction.Update, AuditActions.Update)]
    [InlineData(AuthAction.Delete, AuditActions.Delete)]
    [InlineData(AuthAction.Execute, AuditActions.Execute)]
    public void AuditFlag_MapsEachAction(AuthAction action, AuditActions expected) =>
        Assert.Equal(expected, action.AuditFlag());

    [Fact]
    public void AuditFlag_Throws_ForUnknownAction() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ((AuthAction)99).AuditFlag());
}
