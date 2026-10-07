using PdfKits.Engines;

namespace PdfKits.Tests;

public sealed class IronPdfEngineTests
{
    /// <summary>
    /// IronPDF requires a license key. With IRONPDF_LICENSE_KEY set the engine must render;
    /// without it, the engine must fail with an actionable license message (never an unhandled crash).
    /// </summary>
    [Fact]
    public void Renders_with_license_or_explains_missing_license()
    {
        var engine = new IronPdfEngine();
        const string html = "<!DOCTYPE html><html><body><h1>Iron smoke</h1></body></html>";

        try
        {
            var bytes = engine.Render(html, new PdfRenderOptions { Title = "Iron smoke" });
            Assert.True(TestSupport.IsValidPdf(bytes), "Output is not a PDF.");
            Assert.True(engine.IsLicenseApplied, "A successful render implies an applied license.");
        }
        catch (PdfEngineException ex)
        {
            Assert.Contains("license", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("IRONPDF_LICENSE_KEY", ex.Message);
            Assert.False(engine.IsLicenseApplied);
        }
    }

    [Fact]
    public void Engine_factory_selects_expected_engines()
    {
        Assert.Equal("ironpdf", PdfEngines.Create("iron").Name);
        Assert.Equal("ironpdf", PdfEngines.Create("IronPDF").Name);
        Assert.Equal("dynamicpdf", PdfEngines.Create("dynamic").Name);
        Assert.Equal("dynamicpdf", PdfEngines.Create("cete").Name);

        // "auto" depends on whether IRONPDF_LICENSE_KEY happens to be set in the environment.
        var auto = PdfEngines.Create("auto");
        var hasKey = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("IRONPDF_LICENSE_KEY"));
        Assert.Equal(hasKey ? "ironpdf" : "dynamicpdf", auto.Name);

        Assert.Throws<ArgumentException>(() => PdfEngines.Create("wkhtmltopdf"));
    }
}
