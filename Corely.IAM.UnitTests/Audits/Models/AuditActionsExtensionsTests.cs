using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Models.Extensions;
using Corely.IAM.Security.Constants;

namespace Corely.IAM.UnitTests.Audits.Models;

public class AuditActionsExtensionsTests
{
    [Theory]
    [InlineData(AuthAction.Create, AuditActions.Create)]
    [InlineData(AuthAction.Read, AuditActions.Read)]
    [InlineData(AuthAction.Update, AuditActions.Update)]
    [InlineData(AuthAction.Delete, AuditActions.Delete)]
    [InlineData(AuthAction.Execute, AuditActions.Execute)]
    public void ToAuditActions_MapsEachAction(AuthAction action, AuditActions expected)
    {
        Assert.Equal(expected, action.ToAuditActions());
    }

    [Fact]
    public void ToAuditActions_Throws_ForUnknownAction()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((AuthAction)99).ToAuditActions());
    }

    [Theory]
    [InlineData(AuditActions.Create | AuditActions.Delete, AuthAction.Delete, true)]
    [InlineData(AuditActions.Create | AuditActions.Delete, AuthAction.Read, false)]
    [InlineData(AuditActions.None, AuthAction.Create, false)]
    [InlineData(AuditActions.All, AuthAction.Execute, true)]
    public void Includes_ChecksTheActionFlag(AuditActions actions, AuthAction action, bool expected)
    {
        Assert.Equal(expected, actions.Includes(action));
    }
}
