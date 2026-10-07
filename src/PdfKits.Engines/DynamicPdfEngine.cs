using System.Text.RegularExpressions;
using ceTe.DynamicPDF.HtmlConverter;

namespace PdfKits.Engines;

/// <summary>
/// ceTe.DynamicPDF HTML converter engine. Works without a key (evaluation output),
/// and honors a license supplied via the DYNAMICPDF_LICENSE_KEY environment variable.
/// </summary>
public sealed class DynamicPdfEngine : IPdfEngine
{
    public string Name => "dynamicpdf";

    public DynamicPdfEngine()
    {
    }

    public byte[] Render(string html, PdfRenderOptions o)
    {
        try
        {
            var size = Enum.TryParse<PageSize>(o.PageSize.ToString(), ignoreCase: true, out var pageSize)
                ? pageSize : PageSize.Letter;
            var orientation = o.Orientation == PdfPageOrientation.Portrait
                ? PageOrientation.Portrait
                : PageOrientation.Landscape;
            var uniform = Math.Round((o.MarginLeftPt + o.MarginRightPt + o.MarginTopPt + o.MarginBottomPt) / 4, 2);

            var options = new ConversionOptions(size, orientation, uniform)
            {
                LeftMargin = o.MarginLeftPt,
                RightMargin = o.MarginRightPt,
                TopMargin = o.MarginTopPt,
                BottomMargin = o.MarginBottomPt,
                // Empty string = no header/footer; null would restore the browser default.
                Header = o.HeaderHtml ?? "",
                Footer = o.FooterHtml ?? "",
                IncludeBackground = true,
                OverrideCssPageSize = false,
                Title = o.Title ?? "",
                Author = o.Author ?? "",
                Subject = o.Subject ?? "",
                Keywords = o.Keywords ?? "",
                Creator = o.Creator,
            };

            var bytes = Converter.Convert(ApplyBaseHref(html, o.BasePath), ToBaseUri(o.BasePath), options);
            if (bytes is null || bytes.Length == 0)
                throw new PdfEngineException("DynamicPDF returned an empty document.");
            return bytes;
        }
        catch (PdfEngineException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new PdfEngineException($"DynamicPDF rendering failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// ceTe first navigates the browser to the base URI and waits for that page to finish
    /// loading before injecting the HTML. When the base URI is a directory, Chromium renders a
    /// directory listing, and for large folders (e.g. %TEMP%) that load never completes, so the
    /// conversion times out. PdfKits HTML is self-contained (images are inlined as data URIs by
    /// the renderer), so we always navigate to a tiny sentinel file — which loads instantly —
    /// and expose the caller's base path to the page via a &lt;base&gt; element instead.
    /// </summary>
    private static Uri ToBaseUri(string? basePath)
    {
        var sentinel = Path.Combine(Path.GetTempPath(), ".pdfkits-base.html");
        try
        {
            if (!File.Exists(sentinel))
                File.WriteAllText(sentinel,
                    "<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body></body></html>");
            return new Uri(sentinel);
        }
        catch
        {
            // Sentinel unavailable (locked/readonly temp): fall back to a directory URI.
            return ToDirectoryUri(basePath);
        }
    }

    /// <summary>
    /// Inserts a <c>&lt;base href&gt;</c> element pointing at the caller's base path so relative
    /// references inside the HTML keep resolving against it even though navigation targets the
    /// sentinel file. Skipped when the HTML already declares a base element.
    /// </summary>
    private static string ApplyBaseHref(string html, string? basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath) ||
            html.Contains("<base", StringComparison.OrdinalIgnoreCase))
            return html;

        string dir;
        try
        {
            dir = Path.GetFullPath(basePath);
            if (File.Exists(dir)) dir = Path.GetDirectoryName(dir) ?? dir;
            if (!Directory.Exists(dir)) return html;
            if (!dir.EndsWith(Path.DirectorySeparatorChar)) dir += Path.DirectorySeparatorChar;
        }
        catch
        {
            return html; // Not a usable path; render without a base element.
        }

        var tag = $"<base href=\"{new Uri(dir).AbsoluteUri}\">";
        var head = Regex.Match(html, "<head[^>]*>", RegexOptions.IgnoreCase);
        return head.Success
            ? html.Insert(head.Index + head.Length, tag)
            : tag + html;
    }

    /// <summary>Converts a file/directory path to a file:// URI with trailing slash (required as base).</summary>
    private static Uri ToDirectoryUri(string? basePath)
    {
        var path = string.IsNullOrWhiteSpace(basePath) ? Directory.GetCurrentDirectory() : basePath;
        path = Path.GetFullPath(path);
        if (File.Exists(path)) path = Path.GetDirectoryName(path) ?? path;
        if (!path.EndsWith(Path.DirectorySeparatorChar)) path += Path.DirectorySeparatorChar;
        return new Uri(path);
    }
}
