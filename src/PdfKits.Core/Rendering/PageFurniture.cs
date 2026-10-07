using System.Text;
using PdfKits.Core.Model;

namespace PdfKits.Core.Rendering;

/// <summary>
/// Builds running header/footer HTML templates consumed by the PDF engines.
/// Both engines (Chromium based) replace the standard Chrome print classes:
/// <c>pageNumber</c>, <c>totalPages</c>, <c>date</c>, <c>title</c>, <c>url</c>.
/// Placeholders from <see cref="HeaderFooterSpec"/> are expanded here so templates
/// are engine-independent and fully inline-styled.
/// </summary>
public static class PageFurniture
{
    public static string? BuildHeader(Workbook wb)
    {
        var spec = wb.Page.Header;
        if (!spec.Enabled) return null;
        if (!spec.DividerLine && IsBlank(spec))
            return null;
        return BuildTable(wb, spec, isFooter: false);
    }

    public static string? BuildFooter(Workbook wb)
    {
        var spec = wb.Page.Footer;
        if (!spec.Enabled) return null;
        if (IsBlank(spec) && !spec.ShowPageNumbers)
            return null;
        return BuildTable(wb, spec, isFooter: true);
    }

    private static bool IsBlank(HeaderFooterSpec spec)
        => string.IsNullOrWhiteSpace(spec.Left) && string.IsNullOrWhiteSpace(spec.Center) && string.IsNullOrWhiteSpace(spec.Right);

    private static string BuildTable(Workbook wb, HeaderFooterSpec spec, bool isFooter)
    {
        var t = wb.Theme;
        var left = Expand(wb, spec.Left);
        var center = Expand(wb, spec.Center);
        var right = Expand(wb, spec.Right);

        // Sensible defaults so a plain { enabled: true } already looks professional.
        // A blank footer integrates the HOAWS brand, the generation date, and the
        // running page number, so nothing is left bare on paper.
        if (string.IsNullOrWhiteSpace(left) && string.IsNullOrWhiteSpace(center) && string.IsNullOrWhiteSpace(right))
        {
            if (isFooter)
            {
                left = "HOAWS - Human & Online Administrative / Web Solutions";
                center = "<span class=\"date\"></span>";
                if (spec.ShowPageNumbers)
                    right = PageNumberSpan() + " / " + TotalPagesSpan();
            }
            else
            {
                left = Markup.Plain(wb.Title);
            }
        }
        else if (isFooter && spec.ShowPageNumbers
                 && !right.Contains("pageNumber", StringComparison.Ordinal)
                 && !center.Contains("pageNumber", StringComparison.Ordinal)
                 && !left.Contains("pageNumber", StringComparison.Ordinal))
        {
            right = (string.IsNullOrWhiteSpace(right) ? "" : right + " · ")
                    + PageNumberSpan() + " / " + TotalPagesSpan();
        }

        left = string.IsNullOrWhiteSpace(left) ? "&nbsp;" : left;
        center = string.IsNullOrWhiteSpace(center) ? "&nbsp;" : center;
        right = string.IsNullOrWhiteSpace(right) ? "&nbsp;" : right;

        var border = spec.DividerLine
            ? $"border-{(isFooter ? "top" : "bottom")}:1px solid {t.LineColor};"
            : "";

        var sb = new StringBuilder();
        sb.Append($"<table style=\"width:100%; border-collapse:collapse; {border}")
          .Append($" font-family:{t.BodyFont}; font-size:7.5pt; color:{t.MutedColor};\">")
          .Append("<tr>")
          .Append($"<td style=\"text-align:left; width:38%; padding:{(spec.DividerLine ? "3pt" : "0")} 0 0 0; {borderIfHeader(border)}\">{left}</td>")
          .Append($"<td style=\"text-align:center; width:24%; padding:{(spec.DividerLine ? "3pt" : "0")} 0 0 0;\">{center}</td>")
          .Append($"<td style=\"text-align:right; width:38%; padding:{(spec.DividerLine ? "3pt" : "0")} 0 0 0;\">{right}</td>")
          .Append("</tr></table>");
        return sb.ToString();

        static string borderIfHeader(string border) => ""; // border lives on the table only
    }

    private static string PageNumberSpan() => "<span class=\"pageNumber\"></span>";
    private static string TotalPagesSpan() => "<span class=\"totalPages\"></span>";

    /// <summary>Expands {{placeholders}} in a header/footer cell.</summary>
    private static string Expand(Workbook wb, string? template)
    {
        if (string.IsNullOrWhiteSpace(template)) return "";

        var s = template;
        s = Replace("title", Markup.Plain(wb.Title));
        s = Replace("subtitle", Markup.Plain(wb.Subtitle));
        s = Replace("author", Markup.Plain(wb.Author));
        s = Replace("publisher", Markup.Plain(wb.Publisher));
        s = Replace("version", Markup.Plain(wb.Version));
        s = Replace("copyright", Markup.Plain(wb.ResolvedCopyright));
        s = Replace("date", "<span class=\"date\"></span>");
        s = Replace("pageNumber", PageNumberSpan());
        s = Replace("totalPages", TotalPagesSpan());
        s = Replace("url", "<span class=\"url\"></span>");
        return s;

        string Replace(string key, string value)
            => s.Replace("{{" + key + "}}", value, StringComparison.OrdinalIgnoreCase);
    }
}
