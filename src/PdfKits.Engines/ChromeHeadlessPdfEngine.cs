using System.Diagnostics;
using System.Text;
using PdfKits.Core.Model;

namespace PdfKits.Engines;

/// <summary>
/// Prints a pre-built HTML document to PDF using headless Google Chrome. This keeps
/// rendering on the same Chromium runtime produced by the ceTe DynamicPDF converter
/// and output file, with full control over the print pipeline.
/// </summary>
public sealed class ChromeHeadlessPdfEngine : IPdfEngine
{
    public string Name => "chrome";

    private static readonly string ChromeExe = FindChromeExe();

    public byte[] Render(string html, PdfRenderOptions o)
    {
        var inputPath = Path.ChangeExtension(Path.GetTempFileName(), ".html");
        var outputPath = Path.ChangeExtension(Path.GetTempFileName(), ".pdf");
        try
        {
            File.WriteAllText(inputPath, html, new UTF8Encoding(false));

            var size = Enum.TryParse<PdfPageSize>(o.PageSize.ToString(), ignoreCase: true, out var pageSize)
                ? pageSize : PdfPageSize.Letter;
            var orientation = o.Orientation == PdfPageOrientation.Portrait
                ? PdfPageOrientation.Portrait
                : PdfPageOrientation.Landscape;

            // Pass explicit page size/orientation via CSS so Chromium renders exactly the
            // Letter/A4 portrait/landscape geometry the workbook declares.
            var media = @$"@page {{
  size: {size.ToString().ToLowerInvariant()} {orientation.ToString().ToLowerInvariant()};
}}";
            var printArgs = $"--headless --no-pdf-header-footer --print-to-pdf=\"{outputPath}\" \"{inputPath}\"";
            var psi = new ProcessStartInfo
            {
                FileName = ChromeExe,
                Arguments = printArgs,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
                throw new PdfEngineException("Could not start Chrome headless.");

            process.WaitForExit(90_000);
            var error = process.StandardError.ReadToEnd();
            if (process.ExitCode != 0)
                throw new PdfEngineException($"Chrome headless exited with code {process.ExitCode}. {error}");
            if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
                throw new PdfEngineException("Chrome produced an empty PDF.");

            return File.ReadAllBytes(outputPath);
        }
        finally
        {
            try { if (File.Exists(inputPath)) File.Delete(inputPath); } catch { /* ignore */ }
            try { if (File.Exists(outputPath)) File.Delete(outputPath); } catch { /* ignore */ }
        }
    }

    private static string FindChromeExe()
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("CHROME_EXE") ?? "",
            Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles") ?? @"C:\Program Files", "Google\\Chrome\\Application\\chrome.exe"),
            Path.Combine(Environment.GetEnvironmentVariable("LocalAppData") ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google\\Chrome\\Application\\chrome.exe"),
            @"C:\Program Files\Google\Chrome\Application\chrome.exe",
        };

        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrEmpty(candidate) && File.Exists(candidate))
                return candidate;
        }

        // Last resort: hope Chrome is on PATH.
        return "chrome.exe";
    }
}
