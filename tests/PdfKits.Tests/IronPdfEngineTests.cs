using PdfKits.Engines;

namespace PdfKits.Tests;

public sealed class IronPdfEngineTests
{
    /// <summary>
    /// IronPDF requires a license key when rendering. Without one the engine
    /// fails with an actionable license message (never an unhandled crash).
    /// </summary>
    [Fact]
    public void Renders_without_license_explains_missing_license()
    {
        var engine = new IronPdfEngine();
        const string html = "<!DOCTYPE html><html><body><h1>Iron smoke</h1></body></html>";

        var ex = Assert.Throws<PdfEngineException>(() => engine.Render(html, new PdfRenderOptions { Title = "Iron smoke" }));
        Assert.Contains("license", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IRONPDF_LICENSE_KEY", ex.Message);
    }

    [Fact]
    public void Engine_factory_selects_expected_engines()
    {
        Assert.Equal("ironpdf", PdfEngines.Create("iron").Name);
        Assert.Equal("ironpdf", PdfEngines.Create("IronPDF").Name);
        Assert.Equal("dynamicpdf", PdfEngines.Create("dynamic").Name);
        Assert.Equal("dynamicpdf", PdfEngines.Create("cete").Name);

        // "auto" prefers IronPDF only when IRONPDF_LICENSE_KEY happens to be set in
        // the environment (legacy behaviour); otherwise it falls back to DynamicPDF.
        var auto = PdfEngines.Create("auto");
        var hasKey = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("IRONPDF_LICENSE_KEY"));
        Assert.Equal(hasKey ? "ironpdf" : "dynamicpdf", auto.Name);

        Assert.Throws<ArgumentException>(() => PdfEngines.Create("wkhtmltopdf"));
    }
}
