using System.Globalization;

namespace Corely.IAM.Permissions;

public static class PermissionLabelProvider
{
    public static string GetCrudxLabel(
        bool create,
        bool read,
        bool update,
        bool delete,
        bool execute
    ) =>
        $"{(create ? "C" : "c")}{(read ? "R" : "r")}{(update ? "U" : "u")}{(delete ? "D" : "d")}{(execute ? "X" : "x")}";

    public static string GetName(
        string resourceType,
        Guid resourceId,
        bool create,
        bool read,
        bool update,
        bool delete,
        bool execute
    )
    {
        var resource = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
            resourceType.Replace('_', ' ').ToLowerInvariant()
        );
        if (resourceId != Guid.Empty)
            resource = $"{resource} {resourceId}";

        return $"{resource} : {ActionsLabel(create, read, update, delete, execute)}";
    }

    private static string ActionsLabel(
        bool create,
        bool read,
        bool update,
        bool delete,
        bool execute
    )
    {
        if (create && read && update && delete && execute)
            return "Full Access";

        List<string> verbs =
        [
            .. new (bool Held, string Verb)[]
            {
                (create, "Create"),
                (read, "Read"),
                (update, "Update"),
                (delete, "Delete"),
                (execute, "Execute"),
            }
                .Where(a => a.Held)
                .Select(a => a.Verb),
        ];

        return verbs.Count switch
        {
            0 => "None",
            1 => verbs[0],
            2 => $"{verbs[0]} & {verbs[1]}",
            _ => $"{string.Join(", ", verbs[..^1])}, & {verbs[^1]}",
        };
    }
}
