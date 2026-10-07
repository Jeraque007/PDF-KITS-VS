using IronPdf;
using IronPdf.Rendering;
using IronSoftware.Exceptions;

namespace PdfKits.Engines;

/// <summary>
/// IronPDF (Chromium) engine. IronPDF requires a license key for rendering — supply it
/// via the IRONPDF_LICENSE_KEY environment variable (trial keys are free).
/// </summary>
public sealed class IronPdfEngine : IPdfEngine
{
    public string Name => "ironpdf";
    public string? LicenseKeyEnvironmentVariable => "IRONPDF_LICENSE_KEY";
    public bool IsLicenseApplied { get; }

    public IronPdfEngine()
    {
        var key = Environment.GetEnvironmentVariable(LicenseKeyEnvironmentVariable!);
        if (!string.IsNullOrWhiteSpace(key))
            License.LicenseKey = key;
        IsLicenseApplied = License.IsLicensed;
    }

    public byte[] Render(string html, PdfRenderOptions o)
    {
        try
        {
            var renderer = new ChromePdfRenderer();
            var ro = renderer.RenderingOptions;

            ro.PaperSize = o.PageSize switch
            {
                PdfPageSize.A4 => PdfPaperSize.A4,
                PdfPageSize.A5 => PdfPaperSize.A5,
                PdfPageSize.Legal => PdfPaperSize.Legal,
                PdfPageSize.A3 => PdfPaperSize.A3,
                _ => PdfPaperSize.Letter,
            };
            ro.PaperOrientation = o.Orientation == PdfPageOrientation.Portrait
                ? PdfPaperOrientation.Portrait
                : PdfPaperOrientation.Landscape;

            // IronPDF margins are millimeters (IMargins); our canonical unit is points.
            ro.MarginLeft = PtToMm(o.MarginLeftPt);
            ro.MarginRight = PtToMm(o.MarginRightPt);
            ro.MarginTop = PtToMm(o.MarginTopPt);
            ro.MarginBottom = PtToMm(o.MarginBottomPt);

            ro.CssMediaType = PdfCssMediaType.Print;
            ro.PrintHtmlBackgrounds = true;
            ro.EnableJavaScript = false;
            if (!string.IsNullOrEmpty(o.Title)) ro.Title = o.Title;
            ro.AutoBookmarksFromHeadings = o.GenerateBookmarks;

            if (!string.IsNullOrWhiteSpace(o.HeaderHtml))
                ro.HtmlHeader = new HtmlHeaderFooter
                {
                    HtmlFragment = o.HeaderHtml,
                    LoadStylesAndCSSFromMainHtmlDocument = true,
                };
            if (!string.IsNullOrWhiteSpace(o.FooterHtml))
                ro.HtmlFooter = new HtmlHeaderFooter
                {
                    HtmlFragment = o.FooterHtml,
                    LoadStylesAndCSSFromMainHtmlDocument = true,
                };

            var pdf = string.IsNullOrWhiteSpace(o.BasePath)
                ? renderer.RenderHtmlAsPdf(html)
                : renderer.RenderHtmlAsPdf(html, ToDirectoryUri(o.BasePath));

            var md = pdf.MetaData;
            if (!string.IsNullOrEmpty(o.Author)) md.Author = o.Author;
            if (!string.IsNullOrEmpty(o.Subject)) md.Subject = o.Subject;
            if (!string.IsNullOrEmpty(o.Keywords)) md.Keywords = o.Keywords;
            md.Creator = o.Creator;

            using var buffer = new MemoryStream();
            pdf.Stream.CopyTo(buffer);
            return buffer.ToArray();
        }
        catch (LicensingException ex)
        {
            throw new PdfEngineException(
                "IronPDF requires a license key to render. Set the IRONPDF_LICENSE_KEY environment variable "
                + "(free trial key: https://ironsoftware.com/csharp/pdf/licensing/), or use --engine dynamic instead. "
                + "Details: " + ex.Message,
                ex);
        }
        catch (PdfEngineException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new PdfEngineException($"IronPDF rendering failed: {ex.Message}", ex);
        }
    }

    private static double PtToMm(double points) => Math.Round(points * 25.4 / 72.0, 2);

    private static Uri ToDirectoryUri(string? basePath)
    {
        var path = Path.GetFullPath(basePath!);
        if (File.Exists(path)) path = Path.GetDirectoryName(path) ?? path;
        if (!path.EndsWith(Path.DirectorySeparatorChar)) path += Path.DirectorySeparatorChar;
        return new Uri(path);
    }
}
