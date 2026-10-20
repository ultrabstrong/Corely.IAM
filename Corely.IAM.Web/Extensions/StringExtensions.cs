namespace Corely.IAM.Web.Extensions;

internal static class StringExtensions
{
    extension(string? url)
    {
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
