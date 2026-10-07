using System.Text.Json.Serialization;

namespace PdfKits.Core.Model;

/// <summary>
/// Content block inside a section. JSON discriminator property is "type"
/// (e.g. <c>{"type":"checklist", ...}</c>).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(HeadingBlock), "heading")]
[JsonDerivedType(typeof(ParagraphBlock), "paragraph")]
[JsonDerivedType(typeof(ListBlock), "list")]
[JsonDerivedType(typeof(ChecklistBlock), "checklist")]
[JsonDerivedType(typeof(TableBlock), "table")]
[JsonDerivedType(typeof(RuledLinesBlock), "lines")]
[JsonDerivedType(typeof(PromptBlock), "prompt")]
[JsonDerivedType(typeof(QuoteBlock), "quote")]
[JsonDerivedType(typeof(CalloutBlock), "callout")]
[JsonDerivedType(typeof(DividerBlock), "divider")]
[JsonDerivedType(typeof(SpacerBlock), "spacer")]
[JsonDerivedType(typeof(PageBreakBlock), "pagebreak")]
[JsonDerivedType(typeof(ImageBlock), "image")]
[JsonDerivedType(typeof(RepeatBlock), "repeat")]
public abstract class WorkbookBlock
{
    /// <summary>Optional inline margin above the block, in points.</summary>
    public double? SpacingBeforePt { get; set; }

    /// <summary>Optional inline margin below the block, in points.</summary>
    public double? SpacingAfterPt { get; set; }
}

public sealed class HeadingBlock : WorkbookBlock
{
    /// <summary>Heading level 1–3 (renders as styled sub-heading inside a section).</summary>
    public int Level { get; set; } = 2;

    public string Text { get; set; } = "";

    /// <summary>left | center | right.</summary>
    public string? Align { get; set; }
}

public sealed class ParagraphBlock : WorkbookBlock
{
    public string Text { get; set; } = "";

    /// <summary>default | lead (large intro text) | small (fine print).</summary>
    public string Style { get; set; } = "default";

    public string? Align { get; set; }
}

public sealed class ListBlock : WorkbookBlock
{
    public bool Ordered { get; set; }
    public List<string> Items { get; set; } = new();
}

public sealed class ChecklistBlock : WorkbookBlock
{
    public string? Title { get; set; }
    public List<string> Items { get; set; } = new();

    /// <summary>1–3 columns.</summary>
    public int Columns { get; set; } = 1;
}

public sealed class TableBlock : WorkbookBlock
{
    public List<string>? Headers { get; set; }
    public List<List<string>> Rows { get; set; } = new();

    /// <summary>grid | ruled | plain.</summary>
    public string Style { get; set; } = "ruled";

    public bool HeaderRow { get; set; } = true;

    /// <summary>Fixed row height in points (for writing space); null = auto.</summary>
    public double? RowHeightPt { get; set; }
}

/// <summary>Blank ruled writing lines (journal/notes area).</summary>
public sealed class RuledLinesBlock : WorkbookBlock
{
    public int Count { get; set; } = 10;

    /// <summary>Distance between lines in points (≈24 ≈ college ruled).</summary>
    public double GapPt { get; set; } = 24;

    public string? Label { get; set; }
}

/// <summary>A question followed by ruled answer lines.</summary>
public sealed class PromptBlock : WorkbookBlock
{
    public string Text { get; set; } = "";
    public int Lines { get; set; } = 4;
    public string? Hint { get; set; }
    public bool Numbered { get; set; }
}

public sealed class QuoteBlock : WorkbookBlock
{
    public string Text { get; set; } = "";
    public string? Attribution { get; set; }
}

/// <summary>Colored callout box.</summary>
public sealed class CalloutBlock : WorkbookBlock
{
    /// <summary>note | tip | warning.</summary>
    public string Style { get; set; } = "note";

    public string? Title { get; set; }
    public string Text { get; set; } = "";
}

public sealed class DividerBlock : WorkbookBlock
{
    /// <summary>rule | dots.</summary>
    public string Style { get; set; } = "rule";
}

public sealed class SpacerBlock : WorkbookBlock
{
    public double HeightPt { get; set; } = 12;
}

public sealed class PageBreakBlock : WorkbookBlock
{
}

public sealed class ImageBlock : WorkbookBlock
{
    /// <summary>Relative path, absolute path, http(s) URL or data: URI.</summary>
    public string Src { get; set; } = "";

    public double? WidthIn { get; set; }

    /// <summary>left | center | right.</summary>
    public string Align { get; set; } = "center";

    public string? Alt { get; set; }
}

/// <summary>Repeats nested blocks N times; each iteration starts on a fresh page.</summary>
public sealed class RepeatBlock : WorkbookBlock
{
    public int Times { get; set; } = 1;
    public string? Label { get; set; }
    public List<WorkbookBlock> Blocks { get; set; } = new();
}
