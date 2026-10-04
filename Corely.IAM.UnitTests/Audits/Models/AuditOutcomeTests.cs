using Corely.IAM.Audits.Models;
using Corely.IAM.Models;

namespace Corely.IAM.UnitTests.Audits.Models;

public class AuditOutcomeTests
{
    [Fact]
    public void Of_NamesTheResultCode_AndKeepsOnlyPresentIds()
    {
        var id = Guid.CreateVersion7();

        var outcome = AuditOutcome.Of(ModifyResultCode.NotFoundError, id, null);

        Assert.Equal(nameof(ModifyResultCode.NotFoundError), outcome.ResultCode);
        Assert.Equal([id], outcome.ResourceIds);
        Assert.Null(outcome.ActorUserId);
    }
}
