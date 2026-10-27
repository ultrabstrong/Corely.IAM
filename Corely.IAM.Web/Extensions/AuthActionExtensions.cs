using Corely.IAM.Audits.Enums;
using Corely.IAM.Audits.Models;
using Corely.IAM.Security.Enums;

namespace Corely.IAM.Web.Extensions;

internal static class AuthActionExtensions
{
    extension(AuthAction action)
    {
        public AuditActions AuditFlag() =>
            action switch
            {
                AuthAction.Create => AuditActions.Create,
                AuthAction.Read => AuditActions.Read,
                AuthAction.Update => AuditActions.Update,
                AuthAction.Delete => AuditActions.Delete,
                AuthAction.Execute => AuditActions.Execute,
                _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
            };

        public string Letter() =>
            action switch
            {
                AuthAction.Create => "C",
                AuthAction.Read => "R",
                AuthAction.Update => "U",
                AuthAction.Delete => "D",
                AuthAction.Execute => "X",
                _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
            };
    }
}
