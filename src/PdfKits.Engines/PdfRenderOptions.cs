using PdfKits.Core.Model;

namespace PdfKits.Engines;

public enum PdfPageSize { Letter, A4, A5, Legal, A3 }

public enum PdfPageOrientation { Portrait, Landscape }

/// <summary>
/// Engine-agnostic render settings. Margins are canonically <b>points</b> (72pt = 1in);
/// each engine converts to its native unit (DynamicPDF uses points, IronPDF uses millimeters).
/// </summary>
public sealed class PdfRenderOptions
{
    public PdfPageSize PageSize { get; init; } = PdfPageSize.Letter;
    public PdfPageOrientation Orientation { get; init; } = PdfPageOrientation.Portrait;

    public double MarginLeftPt { get; init; } = 54;
    public double MarginRightPt { get; init; } = 54;
    public double MarginTopPt { get; init; } = 54;
    public double MarginBottomPt { get; init; } = 54;

    /// <summary>Header/footer HTML template (engine injects pageNumber/totalPages/date spans); null/empty = none.</summary>
    public string? HeaderHtml { get; init; }
    public string? FooterHtml { get; init; }

    /// <summary>Directory used to resolve relative resources inside the HTML.</summary>
    public string? BasePath { get; init; }

    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? Subject { get; init; }
    public string? Keywords { get; init; }
    public string Creator { get; init; } = "PdfKits";

    /// <summary>Create PDF bookmarks/outline from headings where the engine supports it.</summary>
    public bool GenerateBookmarks { get; init; } = true;

    public static PdfRenderOptions FromWorkbook(
        Workbook workbook, string? basePath = null, string? headerHtml = null, string? footerHtml = null)
    {
        static double InToPt(double inches) => Math.Round(inches * 72, 2);

        return new PdfRenderOptions
        {
            PageSize = Enum.TryParse<PdfPageSize>(workbook.Page.Size, ignoreCase: true, out var size)
                ? size : PdfPageSize.Letter,
            Orientation = Enum.TryParse<PdfPageOrientation>(workbook.Page.Orientation, ignoreCase: true, out var orientation)
                ? orientation : PdfPageOrientation.Portrait,
            MarginLeftPt = InToPt(workbook.Page.Margins.Left),
            MarginRightPt = InToPt(workbook.Page.Margins.Right),
            MarginTopPt = InToPt(workbook.Page.Margins.Top),
            MarginBottomPt = InToPt(workbook.Page.Margins.Bottom),
            HeaderHtml = headerHtml,
            FooterHtml = footerHtml,
            BasePath = basePath,
            Title = workbook.Title,
            Author = workbook.Author,
            Subject = workbook.Subtitle,
            Keywords = string.Join(", ", new[] { "workbook", "worksheet", workbook.Id }.Where(s => !string.IsNullOrEmpty(s))),
            Creator = "PdfKits",
        };
    }
}
