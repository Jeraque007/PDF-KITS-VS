namespace PdfKits.Core.Rendering;

/// <summary>Page dimensions (inches) shared by layout code.</summary>
public static class PageGeometry
{
    private static readonly Dictionary<string, (double W, double H)> Sizes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["letter"] = (8.5, 11.0),
        ["legal"] = (8.5, 14.0),
        ["a4"] = (8.27, 11.69),
        ["a5"] = (5.83, 8.27),
        ["a3"] = (11.69, 16.54),
    };

    public static (double WidthIn, double HeightIn) PageSizeInches(string size, string orientation)
    {
        if (!Sizes.TryGetValue(size, out var dims))
            dims = Sizes["letter"];
        var landscape = orientation.Equals("landscape", StringComparison.OrdinalIgnoreCase);
        return landscape ? (dims.H, dims.W) : dims;
    }

    /// <summary>Usable content box inside the margins.</summary>
    public static (double WidthIn, double HeightIn) ContentInches(Model.PageSetup page)
    {
        var (w, h) = PageSizeInches(page.Size, page.Orientation);
        return (
            Math.Max(1, w - page.Margins.Left - page.Margins.Right),
            Math.Max(1, h - page.Margins.Top - page.Margins.Bottom));
    }
}
