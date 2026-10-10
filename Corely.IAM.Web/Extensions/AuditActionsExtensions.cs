using Corely.IAM.Audits.Models;
using Corely.IAM.Security.Constants;

namespace Corely.IAM.Web.Extensions;

internal static class AuditActionsExtensions
{
    extension(AuditActions actions)
    {
        public bool Has(AuthAction action) => (actions & action.AuditFlag()) != 0;

        public AuditActions With(AuthAction action, bool included) =>
            included ? actions | action.AuditFlag() : actions & ~action.AuditFlag();
    }
}
