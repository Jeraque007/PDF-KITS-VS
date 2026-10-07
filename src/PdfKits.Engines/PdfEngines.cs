namespace PdfKits.Engines;

/// <summary>Engine selection helpers.</summary>
public static class PdfEngines
{
    public static readonly string[] Names = { "auto", "iron", "ironpdf", "dynamic", "dynamicpdf", "cete", "chrome", "headless" };

    /// <summary>
    /// Creates an engine by name:
    /// "iron"/"ironpdf" → IronPDF, "dynamic"/"dynamicpdf"/"cete" → DynamicPDF,
    /// "chrome"/"headless" → Chromium headless print-to-PDF, "auto"/null → DynamicPDF.
    /// </summary>
    public static IPdfEngine Create(string? name)
    {
        switch (name?.Trim().ToLowerInvariant())
        {
            case "iron":
            case "ironpdf":
                return new IronPdfEngine();

            case "dynamic":
            case "dynamicpdf":
            case "cete":
                return new DynamicPdfEngine();

            case "chrome":
            case "headless":
                return new ChromeHeadlessPdfEngine();

            case null:
            case "":
            case "auto":
                return new DynamicPdfEngine();

            default:
                throw new ArgumentException(
                    $"Unknown engine '{name}'. Valid values: {string.Join(", ", Names)}.");
        }
    }
}
