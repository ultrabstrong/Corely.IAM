using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;

namespace Corely.IAM.UnitTests.Audits;

internal sealed class CapturingAuditProvider : IAuditProvider
{
    public List<CapturedAudit> Calls { get; } = [];

    public async Task<TResult> RecordAsync<TResult>(
        AuditCall call,
        Func<Task<TResult>> operation,
        Func<TResult, AuditOutcome> outcome,
        string operationName = ""
    )
    {
        var result = await operation();
        AuditOutcome? described;
        try
        {
            described = outcome(result);
        }
        catch (NullReferenceException)
        {
            described = null;
        }
        Calls.Add(new CapturedAudit(call, operationName, described));
        return result;
    }

    public async Task RecordAsync(AuditCall call, Func<Task> operation, string operationName = "")
    {
        await operation();
        Calls.Add(new CapturedAudit(call, operationName, null));
    }
}

internal sealed record CapturedAudit(AuditCall Call, string Operation, AuditOutcome? Outcome);
