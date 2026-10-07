using PdfKits.Core.Model;
using PdfKits.Core.Serialization;

namespace PdfKits.Tests;

public sealed class WorkbookLoadingTests
{
    [Fact]
    public void Loads_sample_workbook()
    {
        var wb = TestSupport.LoadSample();

        Assert.Equal("clarity-planner", wb.Id);
        Assert.Equal("The Clarity Planner", wb.Title);
        Assert.Equal(7, wb.Sections.Count);
        Assert.True(wb.Cover.Enabled);
        Assert.True(wb.Page.Footer.Enabled);
        Assert.Equal("Letter", wb.Page.Size);
    }

    [Fact]
    public void Deserializes_polymorphic_blocks()
    {
        var wb = TestSupport.LoadSample();

        var weekly = wb.Sections.Single(s => s.Id == "weekly-pages");
        var repeat = Assert.IsType<RepeatBlock>(weekly.Blocks.Single(b => b is RepeatBlock));
        Assert.Equal(13, repeat.Times);
        Assert.Contains(repeat.Blocks, b => b is TableBlock);
        Assert.Contains(repeat.Blocks, b => b is PromptBlock);

        var start = wb.Sections.Single(s => s.Id == "start-here");
        Assert.Contains(start.Blocks, b => b is ChecklistBlock);
        Assert.Contains(start.Blocks, b => b is QuoteBlock);
        Assert.Contains(start.Blocks, b => b is CalloutBlock);
        Assert.Contains(start.Blocks, b => b is ListBlock { Ordered: true });
    }

    [Fact]
    public void Round_trips_through_json()
    {
        var original = TestSupport.LoadSample();
        var json = WorkbookJson.Serialize(original);
        var back = WorkbookJson.Parse(json);

        Assert.Equal(original.Title, back.Title);
        Assert.Equal(original.Sections.Count, back.Sections.Count);
        Assert.Equal(original.Theme.PrimaryColor, back.Theme.PrimaryColor);

        var repeatBefore = (RepeatBlock)original.Sections.Single(s => s.Id == "weekly-pages").Blocks.Single(b => b is RepeatBlock);
        var repeatAfter = (RepeatBlock)back.Sections.Single(s => s.Id == "weekly-pages").Blocks.Single(b => b is RepeatBlock);
        Assert.Equal(repeatBefore.Times, repeatAfter.Times);
    }

    [Fact]
    public void Unknown_block_type_fails_with_serialization_error()
    {
        const string json = """
            { "id": "x", "title": "T", "sections": [
                { "id": "a", "title": "A", "blocks": [ { "type": "bogus-block" } ] } ] }
            """;

        var ex = Assert.Throws<WorkbookSerializationException>(() => WorkbookJson.Parse(json));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }

    [Fact]
    public void Missing_file_gives_clear_error()
    {
        var ex = Assert.Throws<WorkbookSerializationException>(
            () => WorkbookJson.Load(Path.Combine(TestSupport.RepoRoot, "workbooks", "does-not-exist.json")));
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
