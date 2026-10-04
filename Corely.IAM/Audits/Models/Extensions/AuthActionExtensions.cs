using Corely.IAM.Security.Constants;

namespace Corely.IAM.Audits.Models.Extensions;

internal static class AuthActionExtensions
{
    extension(AuthAction action)
    {
        public AuditActions ToAuditActions() =>
            action switch
            {
                AuthAction.Create => AuditActions.Create,
                AuthAction.Read => AuditActions.Read,
                AuthAction.Update => AuditActions.Update,
                AuthAction.Delete => AuditActions.Delete,
                AuthAction.Execute => AuditActions.Execute,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(action),
                    action,
                    $"Unmapped {nameof(AuthAction)}: {action}"
                ),
            };
    }
}
