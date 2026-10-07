using PdfKits.Core.Rendering;

namespace PdfKits.Tests;

public sealed class HtmlRenderingTests
{
    [Fact]
    public void Renders_full_document_with_cover_toc_and_sections()
    {
        var result = TestSupport.RenderSample();

        Assert.StartsWith("<!DOCTYPE html>", result.Html);
        Assert.Contains("<title>The Clarity Planner</title>", result.Html);
        Assert.Contains("cover-title", result.Html);
        Assert.Contains("cover-kicker", result.Html);
        Assert.Contains("class=\"front-title\">Contents<", result.Html);
        Assert.Contains("class=\"chapter last\"", result.Html);
        Assert.Contains("</html>", result.Html);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Toc_lists_every_section()
    {
        var html = TestSupport.RenderSample().Html;
        var wb = TestSupport.LoadSample();

        var expected = wb.Sections.Count(s => s.InToc);
        var actual = CountOccurrences(html, "class=\"toc-item\"");
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Inline_markup_is_converted_and_escaped()
    {
        var html = TestSupport.RenderSample().Html;

        // "*written in*" is italic markup -> <em>; the asterisks must not survive into the HTML.
        Assert.Contains("<em>written in</em>", html);
        Assert.DoesNotContain("*written in*", html);
    }

    [Fact]
    public void Repeat_block_renders_one_page_per_iteration()
    {
        var html = TestSupport.RenderSample().Html;

        Assert.Contains("Week 1 of 13", html);
        Assert.Contains("Week 13 of 13", html);
        Assert.Contains("Journal entry 1 of 6", html);
        Assert.Contains("Journal entry 6 of 6", html);

        // 13 weekly + 6 journal iterations; every iteration but the last of each
        // group carries the forced page-break class.
        Assert.Equal(19, CountOccurrences(html, "class=\"iteration"));
        Assert.Equal(17, CountOccurrences(html, "class=\"iteration brk\""));
    }

    [Fact]
    public void Numbered_prompts_are_numbered_per_section()
    {
        var html = TestSupport.RenderSample().Html;

        Assert.Contains("prompt-num\">1.</span>", html);
        Assert.Contains("prompt-num\">3.</span>", html);
    }

    [Fact]
    public void Footer_template_is_engine_ready()
    {
        var wb = TestSupport.LoadSample();

        var footer = PageFurniture.BuildFooter(wb);
        Assert.NotNull(footer);
        Assert.Contains("class=\"pageNumber\"", footer);
        Assert.Contains("class=\"totalPages\"", footer);
        Assert.Contains("The Clarity Planner", footer); // {{title}} expanded literally
        Assert.Contains("border-top", footer);          // dividerLine: true

        Assert.Null(PageFurniture.BuildHeader(wb));      // header not enabled
    }

    [Fact]
    public void Footer_shows_page_numbers_by_default_even_with_custom_cells()
    {
        var wb = TestSupport.LoadSample();
        wb.Page.Footer.Left = "{{author}}";
        wb.Page.Footer.Right = null;

        var footer = PageFurniture.BuildFooter(wb);

        Assert.Contains("Alex Mercer", footer);
        Assert.Contains("class=\"pageNumber\"", footer);
    }

    [Fact]
    public void Theme_colors_flow_into_css()
    {
        var wb = TestSupport.LoadSample();
        wb.Theme.PrimaryColor = "#123456";

        var html = WorkbookHtmlRenderer.Render(wb).Html;

        Assert.Contains("#123456", html);
        Assert.Contains(".cover-title", html); // stylesheet present
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }
}
