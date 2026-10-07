using System.Text.RegularExpressions;
using PdfKits.Core.Model;

namespace PdfKits.Core.Validation;

public sealed record ValidationIssue(string Path, string Message, bool IsWarning = false)
{
    public override string ToString()
        => $"{(IsWarning ? "warning" : "error")}: {Path}: {Message}";
}

public sealed class WorkbookValidationException : Exception
{
    public IReadOnlyList<ValidationIssue> Issues { get; }

    public WorkbookValidationException(IReadOnlyList<ValidationIssue> issues)
        : base("Workbook validation failed with " + issues.Count(i => !i.IsWarning) + " error(s): "
               + string.Join("; ", issues.Where(i => !i.IsWarning).Select(i => $"{i.Path}: {i.Message}")))
    {
        Issues = issues;
    }
}

/// <summary>Validates a workbook before rendering so bad products fail fast with clear paths.</summary>
public static class WorkbookValidator
{
    private static readonly string[] PageSizes = { "letter", "a4", "a5", "legal", "a3" };
    private static readonly string[] Orientations = { "portrait", "landscape" };
    private static readonly Regex HexColor = new(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", RegexOptions.Compiled);

    private const int MaxRepeatNesting = 3;
    private const int MaxRepeatTimes = 500;

    public static IReadOnlyList<ValidationIssue> Validate(Workbook workbook, string? basePath = null)
    {
        var issues = new List<ValidationIssue>();
        if (workbook is null)
        {
            issues.Add(new ValidationIssue("$", "Workbook is null."));
            return issues;
        }

        if (string.IsNullOrWhiteSpace(workbook.Id))
            issues.Add(new ValidationIssue("id", "id is required (used for the output file name)."));
        else if (!Regex.IsMatch(workbook.Id, "^[a-z0-9][a-z0-9-]*$"))
            issues.Add(new ValidationIssue("id", "id must be lowercase letters, digits and dashes."));

        if (string.IsNullOrWhiteSpace(workbook.Title))
            issues.Add(new ValidationIssue("title", "title is required."));

        ValidatePage(workbook.Page, issues);
        ValidateTheme(workbook.Theme, issues);

        if (workbook.Sections.Count == 0)
            issues.Add(new ValidationIssue("sections", "At least one section is required."));

        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < workbook.Sections.Count; i++)
        {
            var section = workbook.Sections[i];
            var path = $"sections[{i}]";
            if (string.IsNullOrWhiteSpace(section.Title))
                issues.Add(new ValidationIssue($"{path}.title", "Section title is required."));
            if (string.IsNullOrWhiteSpace(section.Id))
                issues.Add(new ValidationIssue($"{path}.id", "Section id is required (used for anchors)."));
            else if (!seenIds.Add(section.Id))
                issues.Add(new ValidationIssue($"{path}.id", $"Duplicate section id '{section.Id}'."));

            if (section.Blocks.Count == 0)
                issues.Add(new ValidationIssue($"{path}.blocks", "Section has no blocks.", IsWarning: true));

            for (var b = 0; b < section.Blocks.Count; b++)
                ValidateBlock(section.Blocks[b], $"{path}.blocks[{b}]", 0, issues, basePath);
        }

        if (workbook.FrontMatter.TableOfContents && workbook.Sections.Count(s => s.InToc) == 0)
            issues.Add(new ValidationIssue("frontMatter.tableOfContents",
                "Contents page enabled but no section has inToc = true.", IsWarning: true));

        return issues;
    }

    private static void ValidatePage(PageSetup page, List<ValidationIssue> issues)
    {
        if (!PageSizes.Contains(page.Size.ToLowerInvariant()))
            issues.Add(new ValidationIssue("page.size",
                $"Unsupported page size '{page.Size}'. Use one of: {string.Join(", ", PageSizes)}."));
        if (!Orientations.Contains(page.Orientation.ToLowerInvariant()))
            issues.Add(new ValidationIssue("page.orientation",
                $"Unsupported orientation '{page.Orientation}'. Use Portrait or Landscape."));

        void Margin(string name, double value)
        {
            if (value is < 0 or > 4)
                issues.Add(new ValidationIssue($"page.margins.{name.ToLowerInvariant()}", $"Margin {value}in is outside 0–4in."));
            else if (value < 0.3 && (page.Header.Enabled || page.Footer.Enabled))
                issues.Add(new ValidationIssue($"page.margins.{name.ToLowerInvariant()}",
                    "Margins below 0.3in can clip running headers/footers.", IsWarning: true));
        }

        Margin("Top", page.Margins.Top);
        Margin("Right", page.Margins.Right);
        Margin("Bottom", page.Margins.Bottom);
        Margin("Left", page.Margins.Left);
    }

    private static void ValidateTheme(WorkbookTheme theme, List<ValidationIssue> issues)
    {
        foreach (var (name, value) in new[]
        {
            ("primaryColor", theme.PrimaryColor), ("secondaryColor", theme.SecondaryColor),
            ("accentColor", theme.AccentColor), ("inkColor", theme.InkColor),
            ("mutedColor", theme.MutedColor), ("lineColor", theme.LineColor),
            ("surfaceColor", theme.SurfaceColor),
        })
        {
            if (!HexColor.IsMatch(value ?? ""))
                issues.Add(new ValidationIssue($"theme.{name}", $"'{value}' is not a hex color like #1F4E79."));
        }

        if (theme.BaseFontSizePt is < 6 or > 24)
            issues.Add(new ValidationIssue("theme.baseFontSizePt", "Base font size must be between 6 and 24pt."));
        if (theme.LineHeight is < 1 or > 3)
            issues.Add(new ValidationIssue("theme.lineHeight", "Line height must be between 1 and 3."));
        if (string.IsNullOrWhiteSpace(theme.HeadingFont) || string.IsNullOrWhiteSpace(theme.BodyFont))
            issues.Add(new ValidationIssue("theme.headingFont", "Fonts must not be empty."));
    }

    private static void ValidateBlock(WorkbookBlock block, string path, int depth, List<ValidationIssue> issues, string? basePath)
    {
        switch (block)
        {
            case HeadingBlock h:
                if (string.IsNullOrWhiteSpace(h.Text)) issues.Add(new ValidationIssue($"{path}.text", "Heading text is required."));
                if (h.Level is < 1 or > 3) issues.Add(new ValidationIssue($"{path}.level", "Heading level must be 1–3."));
                break;

            case ParagraphBlock p:
                if (string.IsNullOrWhiteSpace(p.Text)) issues.Add(new ValidationIssue($"{path}.text", "Paragraph text is required."));
                if (p.Style is not ("default" or "lead" or "small"))
                    issues.Add(new ValidationIssue($"{path}.style", "Style must be default, lead or small."));
                break;

            case ListBlock l:
                if (l.Items.Count == 0) issues.Add(new ValidationIssue($"{path}.items", "List needs at least one item."));
                break;

            case ChecklistBlock c:
                if (c.Items.Count == 0) issues.Add(new ValidationIssue($"{path}.items", "Checklist needs at least one item."));
                if (c.Columns is < 1 or > 3) issues.Add(new ValidationIssue($"{path}.columns", "Columns must be 1–3."));
                break;

            case TableBlock t:
                if (t.Rows.Count == 0) issues.Add(new ValidationIssue($"{path}.rows", "Table needs at least one row."));
                if (t.Style is not ("grid" or "ruled" or "plain"))
                    issues.Add(new ValidationIssue($"{path}.style", "Table style must be grid, ruled or plain."));
                var cols = t.Headers?.Count ?? (t.Rows.Count > 0 ? t.Rows[0].Count : 0);
                if (t.Headers is not null)
                    for (var r = 0; r < t.Rows.Count; r++)
                        if (t.Rows[r].Count != cols)
                            issues.Add(new ValidationIssue($"{path}.rows[{r}]",
                                $"Row has {t.Rows[r].Count} cells but {cols} columns are expected."));
                break;

            case RuledLinesBlock rl:
                if (rl.Count is < 1 or > 200) issues.Add(new ValidationIssue($"{path}.count", "Line count must be 1–200."));
                if (rl.GapPt is < 12 or > 72) issues.Add(new ValidationIssue($"{path}.gapPt", "Line gap must be 12–72pt."));
                break;

            case PromptBlock pr:
                if (string.IsNullOrWhiteSpace(pr.Text)) issues.Add(new ValidationIssue($"{path}.text", "Prompt text is required."));
                if (pr.Lines is < 0 or > 100) issues.Add(new ValidationIssue($"{path}.lines", "Prompt lines must be 0–100."));
                break;

            case QuoteBlock q:
                if (string.IsNullOrWhiteSpace(q.Text)) issues.Add(new ValidationIssue($"{path}.text", "Quote text is required."));
                break;

            case CalloutBlock co:
                if (co.Style is not ("note" or "tip" or "warning"))
                    issues.Add(new ValidationIssue($"{path}.style", "Callout style must be note, tip or warning."));
                if (string.IsNullOrWhiteSpace(co.Text)) issues.Add(new ValidationIssue($"{path}.text", "Callout text is required."));
                break;

            case SpacerBlock sp:
                if (sp.HeightPt is < 0 or > 500) issues.Add(new ValidationIssue($"{path}.heightPt", "Spacer must be 0–500pt."));
                break;

            case ImageBlock img:
                if (string.IsNullOrWhiteSpace(img.Src))
                    issues.Add(new ValidationIssue($"{path}.src", "Image src is required."));
                else if (basePath is not null
                         && !img.Src.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                         && !Uri.TryCreate(img.Src, UriKind.Absolute, out _)
                         && !File.Exists(Path.Combine(basePath, img.Src)))
                    issues.Add(new ValidationIssue($"{path}.src", $"Image file not found: {img.Src}", IsWarning: true));
                break;

            case RepeatBlock rep:
                if (depth >= MaxRepeatNesting)
                    issues.Add(new ValidationIssue(path, $"repeat blocks may not nest deeper than {MaxRepeatNesting} levels."));
                if (rep.Times is < 1 or > MaxRepeatTimes)
                    issues.Add(new ValidationIssue($"{path}.times", $"repeat times must be 1–{MaxRepeatTimes}."));
                if (rep.Blocks.Count == 0)
                    issues.Add(new ValidationIssue($"{path}.blocks", "repeat block must contain nested blocks."));
                for (var i = 0; i < rep.Blocks.Count; i++)
                    ValidateBlock(rep.Blocks[i], $"{path}.blocks[{i}]", depth + 1, issues, basePath);
                break;
        }
    }
}
