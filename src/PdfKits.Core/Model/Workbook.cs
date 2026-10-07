using System.Text.Json.Serialization;

namespace PdfKits.Core.Model;

/// <summary>
/// Root workbook definition. Loaded from the product JSON files that drive every
/// generated PDF, so a single JSON can be re-branded and re-rendered at will.
/// </summary>
public sealed class Workbook
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string? Author { get; set; }
    public string? Publisher { get; set; }
    public string? Version { get; set; }
    public string Language { get; set; } = "en";

    /// <summary>Footer/attribution line; when null a generic copyright line is built from author + year.</summary>
    public string? Copyright { get; set; }

    public WorkbookTheme Theme { get; set; } = new();
    public PageSetup Page { get; set; } = new();
    public CoverSpec Cover { get; set; } = new();
    public FrontMatter FrontMatter { get; set; } = new();

    public List<Section> Sections { get; set; } = new();

    /// <summary>Computed copyright text used by the copyright page and cover.</summary>
    [JsonIgnore]
    public string ResolvedCopyright => Copyright
        ?? $"© {DateTime.UtcNow.Year} {Author ?? Publisher ?? "All rights reserved."}";
    /// <summary>Optional image-generation instructions. Supplied requests are handled by an <see cref="ImageInstruction"/> and are never silently dropped (see README "Image generation" for the caveat).</summary>
    public IList<ImageInstruction>? ImageInstructions { get; set; }

}

/// <summary>A top-level chapter of the workbook (also the unit listed in the contents page).</summary>
public sealed class Section
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }

    /// <summary>Optional display number; when null the 1-based index is used.</summary>
    public string? Number { get; set; }

    public bool InToc { get; set; } = true;
    public bool StartOnNewPage { get; set; } = true;
    public List<WorkbookBlock> Blocks { get; set; } = new();
}

/// <summary>Full-page cover treatment.</summary>
public sealed class CoverSpec
{
    public bool Enabled { get; set; } = true;

    /// <summary>Small caps line above the title, e.g. "90-DAY GUIDED WORKBOOK".</summary>
    public string? Kicker { get; set; }

    public string? Tagline { get; set; }
    public string? Edition { get; set; }
    public ImageBlock? Logo { get; set; }
}

/// <summary>Front matter toggles (copyright page, contents page).</summary>
public sealed class FrontMatter
{
    public bool CopyrightPage { get; set; } = true;
    public string? Disclaimer { get; set; }
    public bool TableOfContents { get; set; } = true;
    public string TocTitle { get; set; } = "Contents";
}
