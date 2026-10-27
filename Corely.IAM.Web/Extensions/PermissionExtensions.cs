using Corely.IAM.Permissions.Models;

namespace Corely.IAM.Web.Extensions;

internal static class PermissionExtensions
{
    public const string NO_ACTIONS = "No actions";

    extension(Permission permission)
    {
        public string ActionsText()
        {
            var actions = new[]
            {
                (permission.Create, "Create"),
                (permission.Read, "Read"),
                (permission.Update, "Update"),
                (permission.Delete, "Delete"),
                (permission.Execute, "Execute"),
            }
                .Where(a => a.Item1)
                .Select(a => a.Item2)
                .ToList();
            return actions.Count == 0 ? NO_ACTIONS : string.Join(", ", actions);
        }

        public string ScopeText() =>
            permission.ResourceId == Guid.Empty
                ? "every resource"
                : permission.ResourceId.ToString();
    }
}
