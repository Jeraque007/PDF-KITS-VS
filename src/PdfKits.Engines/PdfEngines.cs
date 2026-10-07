namespace PdfKits.Engines;

/// <summary>Engine selection helpers.</summary>
public static class PdfEngines
{
    public static readonly string[] Names = { "auto", "iron", "dynamic" };

    /// <summary>
    /// Creates an engine by name:
    /// "iron"/"ironpdf" → IronPDF, "dynamic"/"dynamicpdf"/"cete" → DynamicPDF,
    /// "auto"/null → IronPDF when IRONPDF_LICENSE_KEY is set, otherwise DynamicPDF.
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

            case null:
            case "":
            case "auto":
                var ironKey = Environment.GetEnvironmentVariable("IRONPDF_LICENSE_KEY");
                return string.IsNullOrWhiteSpace(ironKey) ? new DynamicPdfEngine() : new IronPdfEngine();

            default:
                throw new ArgumentException(
                    $"Unknown engine '{name}'. Valid values: {string.Join(", ", Names)}.");
        }
    }
}
