using PdfKits.Core.Model;
using PdfKits.Core.Rendering;
using PdfKits.Core.Serialization;
using PdfKits.Core.Validation;
using PdfKits.Engines;

/// <summary>Minimal valid workbook written by <c>pdfkits init</c>.</summary>
const string StarterWorkbookJson =
    """
    {
      "id": "my-workbook",
      "title": "My Workbook",
      "subtitle": "A guided workbook",
      "author": "Your Name",
      "version": "1.0",
      "cover": {
        "kicker": "A Guided Workbook",
        "tagline": "Small steps. Big change.",
        "edition": "First Edition"
      },
      "page": {
        "size": "Letter",
        "footer": { "enabled": true, "dividerLine": true }
      },
      "sections": [
        {
          "id": "start-here",
          "title": "Start Here",
          "subtitle": "How to use this workbook",
          "blocks": [
            { "type": "paragraph", "style": "lead", "text": "Welcome. This workbook is yours to write in." },
            { "type": "checklist", "title": "Before you begin", "items": [ "Find a quiet spot", "Grab a pen", "Set a timer" ] },
            { "type": "prompt", "text": "What do you want to get from this workbook?", "lines": 5 }
          ]
        },
        {
          "id": "notes",
          "title": "Notes",
          "blocks": [
            { "type": "lines", "count": 18, "label": "Notes" }
          ]
        }
      ]
    }
    """;

return args switch
{
    [] => Usage(64),
    ["help", ..] or ["--help", ..] or ["-h", ..] => Usage(0),
    ["generate", .. var rest] => RunGenerate(rest),
    ["validate", .. var rest] => RunValidate(rest),
    ["init", .. var rest] => RunInit(rest),
    var other => Fail(64, $"Unknown command '{other[0]}'."),
};

// ---------------------------------------------------------------------------

static int Usage(int code)
{
    Console.WriteLine(
        """
        PdfKits — generate sellable PDF workbooks from JSON definitions.

        usage:
          pdfkits generate <workbook.json> [options]
          pdfkits validate <workbook.json>
          pdfkits init [directory]
          pdfkits help

        generate options:
          --engine auto|iron|cete|chrome   PDF engine (default: auto — Chromium headless
                                        print-to-PDF when Chrome is installed, otherwise
                                        DynamicPDF)

          --out <file.pdf>             Output path (default: <workbook dir>/<id>.pdf)
          --html-out <file.html>       Also write the intermediate HTML (great for debugging)

        engines:
          chrome       Google Chrome --headless --print-to-pdf (needs Chrome installed)
          iron|cete    ceTe DynamicPDF or IronPDF (no license key is required)
          auto         Heuristic default (Chrome when present, otherwise DynamicPDF)

          Images used by the workbook are inlined as data URIs automatically.
          Workbook image-generation requests (<see cref="PdfKits.Core.Model.ImageInstruction"/>)
          are honoured only when an <see cref="PdfKits.Core.IImageGenerator"/> is registered;
          see the README "Image generation" section.
        """);
    return code;
}

static int Fail(int code, string message)
{
    Console.Error.WriteLine("error: " + message);
    return code;
}

static int RunGenerate(string[] rest)
{
    string? input = null, engineName = null, outPath = null, htmlOut = null;

    for (var i = 0; i < rest.Length; i++)
    {
        switch (rest[i])
        {
            case "--engine" when i + 1 < rest.Length:
                engineName = rest[++i];
                break;
            case "--out" or "-o" when i + 1 < rest.Length:
                outPath = rest[++i];
                break;
            case "--html-out" when i + 1 < rest.Length:
                htmlOut = rest[++i];
                break;
            case var a when a.StartsWith('-'):
                return Fail(64, $"Unknown option '{a}'.");
            default:
                if (input is not null) return Fail(64, "Only one input file is supported.");
                input = rest[i];
                break;
        }
    }

    if (input is null)
        return Fail(64, "generate requires a workbook JSON file. See 'pdfkits help'.");

    input = Path.GetFullPath(input);
    if (!File.Exists(input))
        return Fail(2, $"Workbook file not found: {input}");
    var baseDir = Path.GetDirectoryName(input)!;

    Workbook workbook;
    try
    {
        workbook = WorkbookJson.Load(input);
    }
    catch (WorkbookSerializationException ex)
    {
        return Fail(2, ex.Message);
    }

    var issues = WorkbookValidator.Validate(workbook, baseDir);
    foreach (var issue in issues)
        Console.Error.WriteLine(issue.ToString());
    if (issues.Any(i => !i.IsWarning))
        return Fail(1, "Validation failed — fix the errors above and retry.");

    var rendered = WorkbookHtmlRenderer.Render(workbook, new RenderContext { BasePath = baseDir });
    foreach (var warning in rendered.Warnings)
        Console.Error.WriteLine(warning.ToString());

    if (htmlOut is not null)
    {
        var htmlPath = Path.GetFullPath(htmlOut);
        EnsureParentDirectory(htmlPath);
        File.WriteAllText(htmlPath, rendered.Html);
        Console.WriteLine($"html:  {htmlPath} ({rendered.Html.Length:N0} chars)");
    }

    IPdfEngine engine;
    try
    {
        engine = PdfEngines.Create(engineName);
    }
    catch (ArgumentException ex)
    {
        return Fail(64, ex.Message);
    }

    var options = PdfRenderOptions.FromWorkbook(
        workbook,
        basePath: baseDir,
        headerHtml: PageFurniture.BuildHeader(workbook),
        footerHtml: PageFurniture.BuildFooter(workbook));

    byte[] pdf;
    try
    {
        pdf = engine.Render(rendered.Html, options);
    }
    catch (PdfEngineException ex)
    {
        return Fail(2, ex.Message);
    }

    outPath ??= Path.Combine(baseDir, workbook.Id + ".pdf");
    outPath = Path.GetFullPath(outPath);
    EnsureParentDirectory(outPath);
    File.WriteAllBytes(outPath, pdf);

    Console.WriteLine($"pdf:   {outPath} ({pdf.Length:N0} bytes)");
    Console.WriteLine(
        $"done:  engine={engine.Name} size={workbook.Page.Size} {workbook.Page.Orientation.ToLowerInvariant()} "
        + $"sections={workbook.Sections.Count}");
    return 0;
}

static int RunValidate(string[] rest)
{
    if (rest.Length != 1)
        return Fail(64, "validate requires exactly one workbook JSON file.");
    var input = Path.GetFullPath(rest[0]);
    if (!File.Exists(input))
        return Fail(2, $"Workbook file not found: {input}");

    Workbook workbook;
    try
    {
        workbook = WorkbookJson.Load(input);
    }
    catch (WorkbookSerializationException ex)
    {
        return Fail(2, ex.Message);
    }

    var issues = WorkbookValidator.Validate(workbook, Path.GetDirectoryName(input));
    foreach (var issue in issues)
        Console.WriteLine(issue.ToString());

    var errors = issues.Count(i => !i.IsWarning);
    Console.WriteLine(errors == 0
        ? $"ok: '{workbook.Title}' is valid ({issues.Count} warning(s))."
        : $"failed: {errors} error(s), {issues.Count - errors} warning(s).");
    return errors == 0 ? 0 : 1;
}

static int RunInit(string[] rest)
{
    if (rest.Length > 1)
        return Fail(64, "init takes at most one directory argument.");
    var dir = Path.GetFullPath(rest.Length == 1 ? rest[0] : ".");
    var target = Path.Combine(dir, "workbook.json");
    if (File.Exists(target))
        return Fail(2, $"Refusing to overwrite existing file: {target}");

    EnsureParentDirectory(target);
    File.WriteAllText(target, StarterWorkbookJson);
    Console.WriteLine($"created: {target}");
    Console.WriteLine("next:    pdfkits generate " + target);
    return 0;
}

static void EnsureParentDirectory(string filePath)
{
    var dir = Path.GetDirectoryName(filePath);
    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
}
