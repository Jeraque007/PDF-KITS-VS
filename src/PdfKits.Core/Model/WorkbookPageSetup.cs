namespace PdfKits.Core.Model;

/// <summary>Colors, fonts and typographic scale for a workbook product.</summary>
public sealed class WorkbookTheme
{
    public string PrimaryColor { get; set; } = "#1F4E79";
    public string SecondaryColor { get; set; } = "#E8A33D";
    public string AccentColor { get; set; } = "#2E7D6F";
    public string InkColor { get; set; } = "#222A33";
    public string MutedColor { get; set; } = "#6B7280";
    public string LineColor { get; set; } = "#C7D2DC";
    public string SurfaceColor { get; set; } = "#F4F7FA";

    public string HeadingFont { get; set; } = "Georgia, 'Times New Roman', serif";
    public string BodyFont { get; set; } = "'Segoe UI', -apple-system, 'Helvetica Neue', Arial, sans-serif";

    /// <summary>Body text size in points.</summary>
    public double BaseFontSizePt { get; set; } = 10.5;

    /// <summary>Line height multiplier for body text.</summary>
    public double LineHeight { get; set; } = 1.5;
}

/// <summary>Page geometry and running header/footer configuration.</summary>
public sealed class PageSetup
{
    /// <summary>Letter | A4 | A5 | Legal | A3.</summary>
    public string Size { get; set; } = "Letter";

    /// <summary>Portrait | Landscape.</summary>
    public string Orientation { get; set; } = "Portrait";

    /// <summary>Content margins in inches.</summary>
    public MarginsIn Margins { get; set; } = new();

    public HeaderFooterSpec Header { get; set; } = new();
    public HeaderFooterSpec Footer { get; set; } = new();
}

public sealed class MarginsIn
{
    public double Top { get; set; } = 0.75;
    public double Right { get; set; } = 0.75;
    public double Bottom { get; set; } = 0.75;
    public double Left { get; set; } = 0.75;
}

/// <summary>
/// Running header/footer. <c>Left</c>/<c>Center</c>/<c>Right</c> support placeholders:
/// {{title}}, {{subtitle}}, {{author}}, {{publisher}}, {{version}}, {{copyright}},
/// {{date}}, {{pageNumber}}, {{totalPages}}.
/// </summary>
public sealed class HeaderFooterSpec
{
    public bool Enabled { get; set; }

    public bool ShowPageNumbers { get; set; } = true;
    public bool DividerLine { get; set; }
    public string? Left { get; set; }
    public string? Center { get; set; }
    public string? Right { get; set; }
}
