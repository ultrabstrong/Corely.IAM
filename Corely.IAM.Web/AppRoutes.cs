namespace Corely.IAM.Web;

public static class AppRoutes
{
    // Auth (Razor Pages)
    public const string SignIn = "/signin";
    public const string Register = "/register";
    public const string SignOut = "/signout";
    public const string SelectAccount = "/select-account";
    public const string CreateAccount = "/create-account";

    // Legal (Razor Pages)
    public const string Privacy = "/privacy";
    public const string Terms = "/terms";

    // Management (Blazor)
    public const string Dashboard = "/";
    public const string Accounts = "/accounts";
    public const string Users = "/users";
    public const string Groups = "/groups";
    public const string Roles = "/roles";
    public const string Permissions = "/permissions";
}
