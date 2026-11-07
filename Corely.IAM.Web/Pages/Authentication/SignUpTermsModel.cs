namespace Corely.IAM.Web.Pages.Authentication;

internal sealed record SignUpTermsModel(bool Continuing)
{
    public string Lead => Continuing ? "By continuing" : "By signing up";
}
