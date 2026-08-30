using System.Text.RegularExpressions;

namespace Corely.IAM.Web.FunctionalTests.Infrastructure;

/// <summary>
/// Minimal HTML scraping. Deliberately regex rather than a parser: these tests read a handful of
/// known fields out of known pages, and adding an HTML parsing dependency to do it would be more
/// surface than the job needs.
/// </summary>
public static partial class Html
{
    /// <summary>Value of an <c>&lt;input&gt;</c> with the given name attribute.</summary>
    public static string? InputValue(string html, string name)
    {
        var match = Regex.Match(
            html,
            $$"""name="{{Regex.Escape(name)}}"[^>]*value="([^"]*)""",
            RegexOptions.IgnoreCase
        );
        return match.Success ? System.Net.WebUtility.HtmlDecode(match.Groups[1].Value) : null;
    }

    /// <summary>Contents of the first <c>&lt;textarea&gt;</c> on the page.</summary>
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
