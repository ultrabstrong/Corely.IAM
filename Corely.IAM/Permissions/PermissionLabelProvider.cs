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
}
