using PdfKits.Core.Model;
using PdfKits.Core.Validation;

namespace PdfKits.Tests;

public sealed class WorkbookValidatorTests
{
    private static Workbook MinimalWorkbook() => new()
    {
        Id = "test-book",
        Title = "Test Book",
        Sections =
        {
            new Section { Id = "s1", Title = "Section One" },
        },
    };

    [Fact]
    public void Valid_sample_has_no_errors()
    {
        var issues = WorkbookValidator.Validate(TestSupport.LoadSample(), TestSupport.SampleDir);
        Assert.DoesNotContain(issues, i => !i.IsWarning);
    }

    [Fact]
    public void Missing_required_fields_are_reported()
    {
        var issues = WorkbookValidator.Validate(new Workbook());

        Assert.Contains(issues, i => i.Path == "id" && !i.IsWarning);
        Assert.Contains(issues, i => i.Path == "title" && !i.IsWarning);
        Assert.Contains(issues, i => i.Path == "sections" && !i.IsWarning);
    }

    [Fact]
    public void Bad_page_size_and_color_are_reported()
    {
        var wb = MinimalWorkbook();
        wb.Page.Size = "LetterXL";
        wb.Theme.PrimaryColor = "blue";

        var issues = WorkbookValidator.Validate(wb);

        Assert.Contains(issues, i => i.Path == "page.size" && !i.IsWarning);
        Assert.Contains(issues, i => i.Path == "theme.primaryColor" && !i.IsWarning);
    }

    [Fact]
    public void Table_column_mismatch_is_reported_with_path()
    {
        var wb = MinimalWorkbook();
        wb.Sections[0].Blocks.Add(new TableBlock
        {
            Headers = new List<string> { "A", "B" },
            Rows = { new List<string> { "1", "2", "3" } },
        });

        var issues = WorkbookValidator.Validate(wb);

        Assert.Contains(issues, i =>
            i.Path == "sections[0].blocks[0].rows[0]" && !i.IsWarning);
    }

    [Fact]
    public void Repeat_bounds_are_enforced()
    {
        var wb = MinimalWorkbook();
        wb.Sections[0].Blocks.Add(new RepeatBlock { Times = 0, Blocks = { new SpacerBlock() } });

        var issues = WorkbookValidator.Validate(wb);

        Assert.Contains(issues, i => i.Path == "sections[0].blocks[0].times" && !i.IsWarning);
    }

    [Fact]
    public void Duplicate_section_ids_are_reported()
    {
        var wb = MinimalWorkbook();
        wb.Sections.Add(new Section { Id = "s1", Title = "Duplicate" });

        var issues = WorkbookValidator.Validate(wb);

        Assert.Contains(issues, i => i.Path == "sections[1].id" && i.Message.Contains("Duplicate"));
    }
}
