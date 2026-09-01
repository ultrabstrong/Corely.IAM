using System.Text.RegularExpressions;

namespace Corely.IAM.Web.FunctionalTests.Infrastructure;

public static partial class Html
{
    public static string? InputValue(string html, string name)
    {
        var match = Regex.Match(
            html,
            $$"""name="{{Regex.Escape(name)}}"[^>]*value="([^"]*)""",
            RegexOptions.IgnoreCase
        );
        return match.Success ? System.Net.WebUtility.HtmlDecode(match.Groups[1].Value) : null;
    }

    public static string? FirstTextArea(string html)
    {
        var match = TextAreaRegex().Match(html);
        return match.Success
            ? System.Net.WebUtility.HtmlDecode(match.Groups[1].Value).Trim()
            : null;
    }

    [GeneratedRegex(
        "<textarea[^>]*>(.*?)</textarea>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline
    )]
    private static partial Regex TextAreaRegex();
}
