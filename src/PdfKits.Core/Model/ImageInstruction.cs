using System.Text.Json.Serialization;

namespace PdfKits.Core.Model;

/// <summary>A request to *generate* an image for the workbook (distinct from <see cref="ImageBlock"/>, which supplies an existing asset).</summary>
public sealed class ImageInstruction
{
    /// <summary>Natural-language description of the desired image.</summary>
    public string? Prompt { get; set; }

    /// <summary>Existing asset path, when the user already has the image.</summary>
    public string? Source { get; set; }

    /// <summary>Optional cache filename; used by generators that persist the result.</summary>
    public string? OutputFileName { get; set; }
}
