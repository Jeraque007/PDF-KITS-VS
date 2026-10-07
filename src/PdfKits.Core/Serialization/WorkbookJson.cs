using System.Text.Json;
using System.Text.Json.Serialization;
using PdfKits.Core.Model;

namespace PdfKits.Core.Serialization;

/// <summary>Single source of truth for workbook JSON (de)serialization.</summary>
public static class WorkbookJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static Workbook Parse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Workbook>(json, Options)
                   ?? throw new WorkbookSerializationException("Workbook JSON deserialized to null.");
        }
        catch (JsonException ex)
        {
            throw new WorkbookSerializationException(
                $"Invalid workbook JSON at path '{ex.Path}' (line {ex.LineNumber}, byte {ex.BytePositionInLine}): {ex.Message}",
                ex);
        }
    }

    public static Workbook Load(string path)
    {
        if (!File.Exists(path))
            throw new WorkbookSerializationException($"Workbook file not found: {path}");
        return Parse(File.ReadAllText(path));
    }

    public static string Serialize(Workbook workbook)
        => JsonSerializer.Serialize(workbook, Options);

    public static void Save(Workbook workbook, string path)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, Serialize(workbook));
    }
}

public sealed class WorkbookSerializationException : Exception
{
    public WorkbookSerializationException(string message, Exception? inner = null)
        : base(message, inner) { }
}
