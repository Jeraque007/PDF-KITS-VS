using PdfKits.Core.Rendering;
using PdfKits.Engines;

namespace PdfKits.Tests;

public sealed class DynamicPdfEngineTests
{
    [Fact]
    public void Renders_minimal_html_to_valid_pdf()
    {
        var engine = new DynamicPdfEngine();

        var bytes = engine.Render(
            "<!DOCTYPE html><html><body><h1>Smoke</h1></body></html>",
            new PdfRenderOptions { Title = "Smoke" });

        Assert.True(TestSupport.IsValidPdf(bytes), "Output is not a PDF.");
        Assert.True(bytes.Length > 1_000);
        Assert.Equal("dynamicpdf", engine.Name);
    }

    [Fact]
    public void Renders_full_sample_workbook_to_multi_page_pdf()
    {
        var workbook = TestSupport.LoadSample();
        var rendered = TestSupport.RenderSample(workbook);

        var options = PdfRenderOptions.FromWorkbook(
            workbook,
            basePath: TestSupport.SampleDir,
            headerHtml: PageFurniture.BuildHeader(workbook),
            footerHtml: PageFurniture.BuildFooter(workbook));

        var bytes = new DynamicPdfEngine().Render(rendered.Html, options);

        Assert.True(TestSupport.IsValidPdf(bytes), "Output is not a PDF.");
        var pages = TestSupport.PdfPageCount(bytes);
        Assert.True(pages >= 20, $"Expected at least 20 pages, got {pages}.");
        Assert.True(bytes.Length > 50_000, $"Suspiciously small PDF: {bytes.Length} bytes.");
    }

    [Fact]
    public void Honors_page_setup_and_metadata()
    {
        var workbook = TestSupport.LoadSample();
        var rendered = TestSupport.RenderSample(workbook);

        var options = PdfRenderOptions.FromWorkbook(workbook, basePath: TestSupport.SampleDir);
        Assert.Equal(PdfPageSize.Letter, options.PageSize);
        Assert.Equal(PdfPageOrientation.Portrait, options.Orientation);
        Assert.Equal(54, options.MarginTopPt);          // 0.75in
        Assert.Equal("The Clarity Planner", options.Title);
        Assert.Equal("Alex Mercer", options.Author);

        var bytes = new DynamicPdfEngine().Render(rendered.Html, options);
        Assert.True(TestSupport.IsValidPdf(bytes));
    }
}
