namespace Corely.IAM.Web.Extensions;

internal static class StringExtensions
{
    extension(string? url)
    {
        // The URL when it stays on this site, otherwise null: the same rule as IUrlHelper.IsLocalUrl,
        // without the "~/" form, so a return URL can never send a signed in user elsewhere.
        public string? AsLocalUrl()
        {
            if (string.IsNullOrWhiteSpace(url) || url[0] != '/')
                return null;
            if (url.Length == 1)
                return url;
            if (url[1] == '/' || url[1] == '\\')
                return null;
            return url.Any(char.IsControl) ? null : url;
        }
    }
}
