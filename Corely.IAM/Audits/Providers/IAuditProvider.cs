using System.Runtime.CompilerServices;
using Corely.IAM.Audits.Models;

namespace Corely.IAM.Audits.Providers;

public interface IAuditProvider
{
    Task<TResult> RecordAsync<TResult>(
        AuditCall call,
        Func<Task<TResult>> operation,
        Func<TResult, AuditOutcome> outcome,
        [CallerMemberName] string operationName = ""
    );

    Task RecordAsync(
        AuditCall call,
        Func<Task> operation,
        [CallerMemberName] string operationName = ""
    );
}
