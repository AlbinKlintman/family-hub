using System.Net;
using System.Text.RegularExpressions;

namespace WebApp.Tests.Integration;

public static partial class HtmlHelpers
{
    public static string ExtractAntiforgeryToken(string html)
    {
        var inputMatch = AntiforgeryInputRegex().Match(html);
        if (!inputMatch.Success)
        {
            throw new InvalidOperationException("Antiforgery token input not found in response HTML.");
        }

        var valueMatch = ValueAttributeRegex().Match(inputMatch.Value);
        if (!valueMatch.Success)
        {
            throw new InvalidOperationException("Antiforgery token input has no value attribute.");
        }

        return valueMatch.Groups[1].Value;
    }

    public static string ExtractFirstLink(string html)
    {
        var match = HrefRegex().Match(html);
        if (!match.Success)
        {
            throw new InvalidOperationException("No link found in HTML.");
        }

        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    [GeneratedRegex("""<input[^>]*name="__RequestVerificationToken"[^>]*>""", RegexOptions.Singleline)]
    private static partial Regex AntiforgeryInputRegex();

    [GeneratedRegex("value=\"([^\"]*)\"")]
    private static partial Regex ValueAttributeRegex();

    [GeneratedRegex("""href=['"]([^'"]+)['"]""")]
    private static partial Regex HrefRegex();
}
