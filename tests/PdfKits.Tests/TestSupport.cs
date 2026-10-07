[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace PdfKits.Tests;

/// <summary>Shared helpers: locates the repo, loads the sample workbook, inspects PDFs.</summary>
internal static class TestSupport
{
    public static string RepoRoot { get; } = FindRepoRoot();

    public static string SampleWorkbookPath => Path.Combine(RepoRoot, "workbooks", "clarity-planner.json");

    public static string SampleDir => Path.GetDirectoryName(SampleWorkbookPath)!;

    public static Core.Model.Workbook LoadSample()
        => Core.Serialization.WorkbookJson.Load(SampleWorkbookPath);

    public static Core.Rendering.RenderResult RenderSample(Core.Model.Workbook? workbook = null)
    {
        workbook ??= LoadSample();
        return Core.Rendering.WorkbookHtmlRenderer.Render(
            workbook, new Core.Rendering.RenderContext { BasePath = SampleDir });
    }

    public static bool IsValidPdf(byte[]? bytes)
        => bytes is { Length: > 8 }
           && bytes[0] == '%' && bytes[1] == 'P' && bytes[2] == 'D' && bytes[3] == 'F';

    /// <summary>Best-effort page count from the PDF catalog (works for compact and spaced formats).</summary>
    public static int PdfPageCount(byte[] bytes)
    {
        var text = System.Text.Encoding.Latin1.GetString(bytes);
        var m = System.Text.RegularExpressions.Regex.Match(
            text, @"/Type\s*/Pages.{0,2000}?/Count\s+(\d+)",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var count) && count > 0)
            return count;
        return System.Text.RegularExpressions.Regex.Matches(text, @"/Type\s*/Page[^s]").Count;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PdfKits.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate PdfKits.sln above " + AppContext.BaseDirectory);
    }
}
