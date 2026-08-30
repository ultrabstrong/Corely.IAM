namespace Corely.IAM.Web.FunctionalTests.Infrastructure;

/// <summary>
/// The dataset every functional test starts from. Mirrors the shape produced by
/// <c>Corely.IAM.WebApp/DemoSetup/SeedWebAppDemo.ps1</c>, but as code so it runs anywhere with no
/// PATH, config file, or host-level state.
/// </summary>
public static class SeedData
{
    public const string OwnerUsername = "owner";
    public const string OwnerEmail = "owner@example.com";
    public const string OwnerPassword = "Owner!Pass123";

    public const string MemberUsername = "member";
    public const string MemberEmail = "member@example.com";
    public const string MemberPassword = "Member!Pass123";

    public const string AccountName = "Demo Account";
    public const string SecondAccountName = "Second Account";
}
