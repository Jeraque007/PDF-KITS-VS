# PdfKits — Sellable PDF Workbooks from JSON

A small, tested .NET 8 toolchain for generating professional, infinitely repeatable
PDF workbooks (think planners, journals, checklists, guides) from a plain JSON
definition. Two PDF engines can be swapped at the CLI level:

* **DynamicPDF (ceTe)** — keyless, evaluation mode by default (page footers/markers
  identify it as evaluation output), `DYNAMICPDF_LICENSE_KEY` for commercial output.
* **IronPDF** — commercial licence (`IRONPDF_LICENSE_KEY`) required; full-Feature.

Everything is driven by a JSON workbook file; the renderer is isomorphic, so the
same workbook → identical HTML → either engine's PDF. No PDF library code ever
lives in the workbook authoring path; workbooks are plain data.

## Repository layout

| Path | Purpose |
|---|---|
| `src/PdfKits.Core` | Model (Workbook, blocks, page setup), JSON serialization, validation, HTML rendering (CSS, inline markup, header/footer fixtures). |
| `src/PdfKits.Engines` | `IPdfEngine` abstraction; `DynamicPdfEngine`, `IronPdfEngine`, `PdfEngines` factory. |
| `src/PdfKits.Cli` | `pdfkits generate / validate / init / help` console app. |
| `tests/PdfKits.Tests` | xUnit suite (24 tests) — rendering, validation, engines. |
| `workbooks/clarity-planner.json` | Sample 13-week planning workbook + generated PDF. |
| `tools/ApiProbe` | Scratch console for probing third-party libraries (keep out of CI). |

## Quick start

Prerequisites:

* .NET SDK 8 (8.0.425 confirmed; add it to PATH, or call the full path: `%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe`).
* Visual Studio Code with the C# Dev Kit / preview extensions.

```bat
dotnet build PdfKits.sln
dotnet test tests\PdfKits.Tests\PdfKits.Tests.csproj   # 24/24 - expect ~34s
dotnet run --project src\PdfKits.Cli -- generate workbooks/clarity-planner.json
```

The workbook at `workbooks/clarity-planner.json` renders to a 29-page Letter PDF,
including cover, table of contents, repeated weekly/journal blocks, footers with
page numbers, and inline `*italic*` / `**bold**` text.

## CLI reference

```
pdfkits generate <workbook.json> [options]
pdfkits validate <workbook.json>
pdfkits init [directory]
pdfkits help
```

`generate` options:

| Option | Meaning |
|---|---|
| `--engine auto\|iron\|dynamic` | PDF engine (default `auto`: IronPDF when `IRONPDF_LICENSE_KEY` is set, else DynamicPDF). |
| `--out <file.pdf>` | Output path (default `<workbook dir>/<id>.pdf`). |
| `--html-out <file.html>` | Also write the intermediate HTML for debugging. |

`validate` reports every issue (errors and warnings) and exits non-zero if any
non-warning issue exists — use it in CI before generating.

## Workbook JSON authoring guide

A workbook is a single JSON object. Comments are **not** supported (JSON, not
JSONC); delete keys you do not need. All lengths in the `page` section are inches.

```jsonc
{
  "id": "clarity-planner",          // unique slug; used for the default output file
  "title": "The Clarity Planner",   // document title + PDF metadata
  "subtitle": "How to get the most from this workbook",
  "author": "Alex Mercer",
  "version": "1.0",
  "cover": {                        // front page
    "kicker": "A 13-week planning system",
    "tagline": "Small steps. Big change.",
    "edition": "First Edition"
  },
  "page": {
    "size": "Letter",               // Letter | A4 | A5 | Legal | A3
    "orientation": "portrait",
    "margins": { "top": 0.75, "right": 0.75, "bottom": 0.75, "left": 0.75 }
  },
  "sections": [ ... ]               // one section = one PDF part + TOC entry
}
```

### Sections

Each section owns its title, subtitle, blocks, and a table-of-contents flag.
Sections render page breaks between them; the first section may optionally be
the cover (see `WorkbookHeader`/`PageFurniture` for the HTML fixtures it
consumes).

```jsonc
{
  "id": "first-week",
  "inToc": true,                     // include in the generated table of contents
  "title": "The rhythm",
  "subtitle": "How the weeks stack up",
  "blocks": [ ... ]
}
```

### Block types

| Type | Key fields | Notes |
|---|---|---|
| `heading` | `level` (1–3), `text`, `align` | Renders as styled heading; makes bookmarks |
| `paragraph` | `text`, `style` (default/lead/small), `align` | `**bold**` / `*italic*` markup supported |
| `list` | `ordered`, `items[]` | `**`/`**` applies to list items |
| `checklist` | `title`, `items[]`, `columns` (1–3) | Checkbox grid |
| `table` | `headers[]`, `rows[][]`, `style` (grid/ruled/plain), `headerRow` | |
| `lines` | `count`, `label` | Ruled writing lines |
| `prompt` | `text`, `label`, `lines` (count of blank lines) | Fill-in prompt; numbered per section |
| `quote` | `quote` (citation), `attribution` | |
| `callout` | `title`, `text`, `style` (tip/tip-important/warning) | |
| `divider` | — | `<hr>` |
| `spacer` | `heightPt` | |
| `pagebreak` | — | Forced break |
| `image` | `src` (path or `data:`), `align`, `widthIn` | Local files resolved relative to `basePath`; rendered as base64 data URIs so no network access is ever needed |
| `repeat` | `label`, `times`, `blocks[]` | Repeats its blocks with `{n}`/`{total}` numbering |

Block JSON uses a `"type"` discriminator; `ImageBlock` is the only block with
relative-path semantics (its `src` is resolved against the workbook's
`basePath` when rendering via DynamicPDF, but the result is always a
self-contained data URI).

### Inline markup

Text is HTML-escaped, then parsed for markers:

* `**bold**` → `<strong>bold</strong>`
* `*italic*` → `<em>italic</em>`

Newlines in block (`<p>`) text become `<br>` (use `Block()` for that).

### Theme

```jsonc
"theme": {
  "primaryColor": "#2f6f4f",
  "secondaryColor": "#5b7a5e",
  "accentColor": "#c7d2dc",
  "fontFamily": "Segoe UI, Helvetica, Arial, sans-serif",
  "fontSizePt": 10.5,
  "a4TopMarginIn": 0.75
}
```

Margins can be overridden per-page via `Theme` (used by the CSS generator) or
via the workbook `page.margins`.
## Rendering pipeline

1. `WorkbookJson.Load` → validated model.
2. `WorkbookHtmlRenderer.Render` → complete, self-contained HTML:
   * CSS built from `Theme` + block styles.
   * Inline markup escaped+parsed (`**`/`*`).
   * Local images inlined as base64 data URIs; remote HTTP(S) images pass through
     with a *warning*.
   * Header/footer rendered from `{{title}}`, `{{pageNumber}}`, `{{totalPages}}`,
     `{{date}}` placeholders (engine-specific spans).
3. Engine renders the HTML to PDF; header/footer/page-number spans are injected
   by the engine itself (DynamicPDF via `ConversionOptions.Header/Footer`).

## Engines & licensing

| Engine | Key | When to use |
|---|---|---|
| `DynamicPDF` (default "auto" fallback) | `DYNAMICPDF_LICENSE_KEY` (optional) | Dev, demos, evaluation. Output carries evaluation marks and may be watermarked. |
| `IronPDF` | `IRONPDF_LICENSE_KEY` (required) | Production/sellable output. |

`auto` picks IronPDF when `IRONPDF_LICENSE_KEY` is present, otherwise
DynamicPDF. **Before selling a workbook, generate once with the engine you intend
to ship under** — evaluation output must never be distributed as-is.

For IronPDF, the key is set in the environment of the CLI process:

```bat
set IRONPDF_LICENSE_KEY=<your-key>
dotnet run --project src\PdfKits.Cli -- generate --engine iron workbooks/cloud-starter.json
```

DynamicPDF prints a warning but still renders; set `DYNAMICPDF_LICENSE_KEY` to
remove evaluation marks. Both engines honour `IRONPDF_LICENSE_KEY` / `DYNAMICPDF_LICENSE_KEY`
via the engine providers — see the README `Licensing` section for your exact plan.

## Testing

```bat
dotnet test tests\PdfKits.Tests\PdfKits.Tests.csproj
```

24 tests: sample workbook rendering (HTML assertions), validation of JSON
workbooks, DynamicPDF and IronPDF engine contracts, and inline-markup
conversions. Tests are run serially (collection-level single-threaded behavior);
expect ~34s.

## Diagnostics

* `tools/ApiProbe` — console app used to probe third-party library behaviour
  (JSON docs, conversion timeouts, resource cleanup). Never part of the product.
* DynamicPDF writes a `chrome_debug.log` into `%TEMP%\dpdfHtmlConverter_v...`
  with Chromium console output when a conversion stalls.
* A tiny sentinel (`%TEMP%\.pdfkits-base.html`) is used as the ceTe navigation
  target so large working folders can never stall a conversion.

