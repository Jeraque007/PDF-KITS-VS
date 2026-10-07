using System.Net;
using System.Text.RegularExpressions;

namespace PdfKits.Core.Rendering;

/// <summary>
/// Tiny, safe inline markup for workbook text: HTML-escapes everything first, then
/// supports <c>**bold**</c> and <c>*italic*</c>. Newlines become line breaks in block contexts.
/// </summary>
public static class Markup
{
    private static readonly Regex Bold = new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
    private static readonly Regex Italic = new(@"(?<!\*)\*(?!\s)(.+?)(?<!\s)\*(?!\*)", RegexOptions.Compiled);

    /// <summary>Escapes text and applies bold/italic markers. Use for single-line inline content.</summary>
    public static string Inline(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var s = WebUtility.HtmlEncode(text);
        s = Bold.Replace(s, "<strong>$1</strong>");
        s = Italic.Replace(s, "<em>$1</em>");
        return s;
    }

    /// <summary>Same as <see cref="Inline"/> but turns newlines into &lt;br&gt; lines.</summary>
    public static string Block(string? text)
        => string.IsNullOrEmpty(text) ? "" : Inline(text).Replace("\n", "<br>");

    /// <summary>Escapes only (no markup) — for literals placed into attribute-like contexts.</summary>
    public static string Plain(string? text) => WebUtility.HtmlEncode(text ?? "");
}
