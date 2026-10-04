using Corely.IAM.Security.Constants;

namespace Corely.IAM.Audits.Models.Extensions;

internal static class AuditActionsExtensions
{
    extension(AuditActions actions)
    {
        public bool Includes(AuthAction action) => (actions & action.ToAuditActions()) != 0;
    }
}
