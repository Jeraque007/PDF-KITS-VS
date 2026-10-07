using PdfKits.Core.Model;

namespace PdfKits.Core;

/// <summary>Handles workbook image-generation instructions (e.g. supplied assets or text-to-image prompts).</summary>
public interface IImageGenerator
{
    /// <summary>True when this generator can honor an instruction.</summary>
    bool CanHandle(ImageInstruction instruction);

    /// <summary>Honors the instruction.</summary>
    /// <param name="instruction">The instruction to honor.</param>
    /// <param name="dataUri">A data: URI for the resulting image, or null when the instruction could not be honored.</param>
    /// <returns>True when the instruction was honored; false when it could not be honored.</returns>
    bool Generate(ImageInstruction instruction, out string? dataUri);
}

/// <summary>No-op generator: never produces an image. Use it when the user supplies generation instructions but has no generator wired.</summary>
public sealed class NoOpImageGenerator : IImageGenerator
{
    public bool CanHandle(ImageInstruction i) => true;

    public bool Generate(ImageInstruction i, out string? dataUri)
    {
        // Deliberately not generating: no image generator is configured for this build.
        dataUri = null;
        return false;
    }
}

/// <summary>Resolves an explicit asset referenced by an instruction. This honours supplied assets without limiting the request to a fixed set of mediums.</summary>
public sealed class FileBasedImageGenerator : IImageGenerator
{
    private readonly string _basePath;

    public FileBasedImageGenerator(string? basePath = null)
        => _basePath = string.IsNullOrEmpty(basePath) ? Directory.GetCurrentDirectory() : basePath;

    public bool CanHandle(ImageInstruction i) => !string.IsNullOrEmpty(i.Source);

    public bool Generate(ImageInstruction i, out string? dataUri)
    {
        var source = i.Source;
        if (string.IsNullOrEmpty(source))
        {
            dataUri = null;
            return false;
        }

        var full = Path.GetFullPath(Path.Combine(_basePath, source));
        if (!File.Exists(full))
        {
            dataUri = null;
            return false;
        }

        try
        {
            var bytes = File.ReadAllBytes(full);
            dataUri = $"data:{MimeFor(Path.GetExtension(full))};base64,{Convert.ToBase64String(bytes)}";
            return true;
        }
        catch
        {
            dataUri = null;
            return false;
        }
    }

    private static string MimeFor(string ext) => ext.ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".svg" => "image/svg+xml",
        ".bmp" => "image/bmp",
        ".ico" => "image/x-icon",
        _ => "application/octet-stream",
    };
}
