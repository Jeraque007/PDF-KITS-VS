using System.Diagnostics;
using ceTe.DynamicPDF.HtmlConverter;

// Shorten static timeouts via reflection so failures come fast.
try
{
    typeof(ConversionOptions).GetProperty("DefaultPageLoadTimeout")!.SetValue(null, 15000);
    typeof(ConversionOptions).GetProperty("DefaultConversionTimeout")!.SetValue(null, 30000);
    Console.WriteLine("shortened timeouts");
}
catch (Exception ex) { Console.WriteLine("timeout set failed: " + ex.Message); }

ConversionOptions.Logging.Level = LogLevel.Complete;
ConversionOptions.Logging.LogFilePath = @"f:\Jeraque\PDF KITS VS\output\cete-matrix.log";

var html = "<!DOCTYPE html><html><head><style>h1{color:#2f6f4f}</style></head><body><h1>Smoke</h1></body></html>";

var smallDir = new Uri("file:///" + @"C:\Users\User\AppData\Local\Temp\pkits-smallbase\".Replace('\\', '/'));
var smallFile = new Uri("file:///" + @"C:\Users\User\AppData\Local\Temp\pkits-smallbase\base.html".Replace('\\', '/'));
var hugeDir = new Uri(Path.GetTempPath());

var cases = new (string Name, Uri Base)[]
{
    ("C file-base   (1st)", smallFile),
    ("D small-dir   (2nd)", smallDir),
    ("E huge-dir    (3rd)", hugeDir),
};

foreach (var (name, basePath) in cases)
{
    var sw = Stopwatch.StartNew();
    try
    {
        var opts = new ConversionOptions(PageSize.Letter, PageOrientation.Portrait, 54);
        var bytes = Converter.Convert(html, basePath, opts);
        Console.WriteLine($"{name}: OK {bytes.Length} bytes in {sw.Elapsed}");
    }
    catch (Exception ex)
    {
        var msg = ex.Message.Length > 100 ? ex.Message.Substring(0, 100) : ex.Message;
        Console.WriteLine($"{name}: FAIL in {sw.Elapsed}: {msg}");
    }
    finally { try { Converter.ReleaseResources(); } catch { } }
}
