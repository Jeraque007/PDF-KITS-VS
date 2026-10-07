using System.Globalization;
using System.Text;
using PdfKits.Core;
using PdfKits.Core.Model;
using PdfKits.Core.Validation;

namespace PdfKits.Core.Rendering;

/// <summary>Inputs for a single render pass.</summary>
public sealed class RenderContext
{
    /// <summary>Directory used to resolve relative image paths.</summary>
    public string BasePath { get; init; } = Directory.GetCurrentDirectory();
}

public sealed record RenderResult(string Html, IReadOnlyList<ValidationIssue> Warnings);

/// <summary>
/// Turns a <see cref="Workbook"/> into a single self-contained HTML document (print CSS embedded,
/// images inlined as data URIs) ready to hand to any Chromium-based PDF engine.
/// </summary>
public static class WorkbookHtmlRenderer
{
    public static RenderResult Render(Workbook workbook, RenderContext? context = null, IImageGenerator? imageGenerator = null)
        => new Builder(workbook, context ?? new RenderContext(), imageGenerator).Build();

    private static string N(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    private sealed class Builder
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private readonly Workbook _wb;
        private readonly RenderContext _ctx;
        private readonly IImageGenerator? _imageGenerator;
        private readonly List<ValidationIssue> _warnings = new();
        private int _promptCounter;

        public Builder(Workbook wb, RenderContext ctx, IImageGenerator? imageGenerator)
        {
            _wb = wb;
            _ctx = ctx;
            _imageGenerator = imageGenerator;
        }

        public RenderResult Build()
        {
            PrepareImageInstructions();

            var sb = new StringBuilder(64 * 1024);
            sb.Append("<!DOCTYPE html>\n<html lang=\"").Append(Markup.Plain(_wb.Language)).Append("\">\n<head>\n");
            sb.Append("<meta charset=\"utf-8\">\n");
            sb.Append("<meta name=\"generator\" content=\"PdfKits\">\n");
            sb.Append("<title>").Append(Markup.Plain(_wb.Title)).Append("</title>\n");
            sb.Append("<style>\n").Append(WorkbookCss.Build(_wb)).Append("</style>\n</head>\n<body>\n");

            // Front matter always breaks; the final section never does.
            if (_wb.Cover.Enabled) sb.Append(BuildCover());
            if (_wb.FrontMatter.CopyrightPage) sb.Append(BuildCopyright());
            if (_wb.FrontMatter.TableOfContents) sb.Append(BuildToc());

            for (var i = 0; i < _wb.Sections.Count; i++)
                sb.Append(BuildSection(_wb.Sections[i], i, isLast: i == _wb.Sections.Count - 1));

            sb.Append("</body>\n</html>");
            return new RenderResult(sb.ToString(), _warnings);
        }

        /// <summary>Honours image-generation instructions. A request is explicitly reported when no generator is configured, so nothing is silently dropped.</summary>
        private void PrepareImageInstructions()
        {
            if (_wb.ImageInstructions is null || _wb.ImageInstructions.Count == 0)
                return;

            if (_imageGenerator is null)
            {
                _warnings.Add(new ValidationIssue("imageInstructions",
                    "Image generation was requested, but no IImageGenerator is configured. Supplied generation requests will not be honoured until a generator is registered.",
                    IsWarning: true));
                return;
            }

            var handled = 0;
            foreach (var instruction in _wb.ImageInstructions)
            {
                if (instruction.Source is not null)
                {
                    if (_imageGenerator.Generate(instruction, out var dataUri) && dataUri is not null)
                    {
                        handled++;
                        continue;
                    }
                    _warnings.Add(new ValidationIssue("imageInstructions",
                        $"Supplied asset could not be resolved for an image-generation instruction ({instruction.Source}).",
                        IsWarning: true));
                }
                else if (instruction.Prompt is not null)
                {
                    _warnings.Add(new ValidationIssue("imageInstructions",
                        $"Image generation was requested with prompt '{(instruction.Prompt.Length > 80 ? instruction.Prompt[..80] + "…" : instruction.Prompt)}' but only a file-based generator is configured. Supply an existing asset, register a capable generator, or remove the instruction.",
                        IsWarning: true));
                }
            }

            if (handled > 0)
                _warnings.Add(new ValidationIssue("imageInstructions",
                    $"Honoured {handled} supplied asset(s) for image-generation instructions.", IsWarning: false));
        }

        private string BuildCover()
        {
            var c = _wb.Cover;
            var sb = new StringBuilder();
            sb.Append("<section class=\"cover\">\n  <div class=\"cover-band\"></div>\n  <div class=\"cover-body\">\n");
            if (c.Logo is not null && TryResolveImage(c.Logo.Src, "cover.logo", out var logo, out _))
                sb.Append("    <img class=\"cover-logo\" src=\"").Append(logo)
                  .Append("\" alt=\"").Append(Markup.Plain(c.Logo.Alt ?? _wb.Title)).Append("\">\n");
            if (!string.IsNullOrWhiteSpace(c.Kicker))
                sb.Append("    <div class=\"cover-kicker\">").Append(Markup.Inline(c.Kicker)).Append("</div>\n");
            sb.Append("    <h1 class=\"cover-title\">").Append(Markup.Inline(_wb.Title)).Append("</h1>\n");
            sb.Append("    <div class=\"cover-rule\"></div>\n");
            if (!string.IsNullOrWhiteSpace(_wb.Subtitle))
                sb.Append("    <div class=\"cover-subtitle\">").Append(Markup.Inline(_wb.Subtitle)).Append("</div>\n");
            if (!string.IsNullOrWhiteSpace(c.Tagline))
                sb.Append("    <div class=\"cover-tagline\">").Append(Markup.Inline(c.Tagline)).Append("</div>\n");
            sb.Append("  </div>\n  <div class=\"cover-foot\">\n");
            if (!string.IsNullOrWhiteSpace(_wb.Author))
                sb.Append("    <div class=\"cover-author\">").Append(Markup.Inline(_wb.Author)).Append("</div>\n");
            var edition = c.Edition ?? _wb.Version;
            if (!string.IsNullOrWhiteSpace(edition))
                sb.Append("    <div class=\"cover-edition\">").Append(Markup.Inline(edition)).Append("</div>\n");
            sb.Append("  </div>\n  <div class=\"cover-band bottom\"></div>\n</section>\n");
            return sb.ToString();
        }

        private string BuildCopyright()
        {
            var f = _wb.FrontMatter;
            var disclaimer = f.Disclaimer
                ?? "This workbook is provided for general informational and educational purposes only. It is not a substitute for professional medical, financial, legal, or psychological advice. No results are guaranteed; the author and publisher accept no liability for decisions made using this material.";
            var sb = new StringBuilder();
            sb.Append("<section class=\"front copyright\">\n");
            sb.Append("  <div class=\"cr-title\">").Append(Markup.Inline(_wb.Title)).Append("</div>\n");
            sb.Append("  <p>").Append(Markup.Inline(_wb.ResolvedCopyright)).Append("</p>\n");

            var details = new List<string>();
            if (!string.IsNullOrWhiteSpace(_wb.Version)) details.Add("Version " + _wb.Version);
            if (!string.IsNullOrWhiteSpace(_wb.Publisher)) details.Add("Published by " + _wb.Publisher);
            details.Add($"Formatted for {_wb.Page.Size} {_wb.Page.Orientation}");
            sb.Append("  <p>").Append(Markup.Inline(string.Join(" · ", details))).Append("</p>\n");

            sb.Append("  <p>").Append(Markup.Block(disclaimer)).Append("</p>\n");
            sb.Append("  <p>All rights reserved. No part of this publication may be reproduced, stored, or transmitted in any form without prior written permission of the publisher, except for brief quotations used in reviews and for personal use by the original purchaser.</p>\n");
            sb.Append("</section>\n");
            return sb.ToString();
        }

        private string BuildToc()
        {
            var sb = new StringBuilder();
            sb.Append("<section class=\"front\">\n");
            sb.Append("  <div class=\"front-title\">").Append(Markup.Inline(_wb.FrontMatter.TocTitle)).Append("</div>\n");
            sb.Append("  <hr class=\"front-rule\">\n");
            sb.Append("  <ol class=\"toc-list\">\n");
            var n = 0;
            for (var i = 0; i < _wb.Sections.Count; i++)
            {
                var s = _wb.Sections[i];
                if (!s.InToc) continue;
                n++;
                var num = s.Number ?? n.ToString("00", Inv);
                sb.Append("    <li class=\"toc-item\">")
                  .Append("<span class=\"toc-num\">").Append(Markup.Inline(num)).Append("</span>")
                  .Append("<span class=\"toc-text\">").Append(Markup.Inline(s.Title));
                if (!string.IsNullOrWhiteSpace(s.Subtitle))
                    sb.Append("<br><span class=\"toc-sub\">").Append(Markup.Inline(s.Subtitle)).Append("</span>");
                sb.Append("</span></li>\n");
            }
            sb.Append("  </ol>\n</section>\n");
            return sb.ToString();
        }

        private string BuildSection(Section s, int index, bool isLast)
        {
            var cls = isLast ? "chapter last" : s.StartOnNewPage ? "chapter" : "chapter cont";
            var num = s.Number ?? $"SECTION {(index + 1).ToString("00", Inv)}";
            _promptCounter = 0;

            var sb = new StringBuilder();
            sb.Append("<section class=\"").Append(cls).Append("\" id=\"")
              .Append(Markup.Plain(string.IsNullOrWhiteSpace(s.Id) ? "section-" + (index + 1) : s.Id)).Append("\">\n");
            sb.Append("  <header class=\"chapter-head\">\n");
            sb.Append("    <div class=\"chapter-num\">").Append(Markup.Inline(num)).Append("</div>\n");
            sb.Append("    <h1 class=\"chapter-title\">").Append(Markup.Inline(s.Title)).Append("</h1>\n");
            if (!string.IsNullOrWhiteSpace(s.Subtitle))
                sb.Append("    <div class=\"chapter-sub\">").Append(Markup.Inline(s.Subtitle)).Append("</div>\n");
            sb.Append("    <hr class=\"chapter-rule\">\n");
            sb.Append("  </header>\n");
            AppendBlocks(sb, s.Blocks, $"sections[{index}].blocks", depth: 0);
            sb.Append("</section>\n");
            return sb.ToString();
        }

        private void AppendBlocks(StringBuilder sb, List<WorkbookBlock> blocks, string path, int depth)
        {
            for (var i = 0; i < blocks.Count; i++)
                AppendBlock(sb, blocks[i], $"{path}[{i}]", depth);
        }

        private void AppendBlock(StringBuilder sb, WorkbookBlock block, string path, int depth)
        {
            var sp = SpacingStyle(block);
            switch (block)
            {
                case HeadingBlock h:
                {
                    var lvl = Math.Clamp(h.Level, 1, 3);
                    sb.Append("<h").Append(lvl).Append(" class=\"h").Append(lvl).Append(AlignClass(h.Align)).Append('"')
                      .Append(sp).Append('>').Append(Markup.Inline(h.Text)).Append("</h").Append(lvl).Append(">\n");
                    break;
                }

                case ParagraphBlock p:
                {
                    var cls = (p.Style == "default" ? "" : " " + p.Style) + AlignClass(p.Align);
                    sb.Append("<p class=\"").Append(cls.Trim()).Append('"').Append(sp).Append('>')
                      .Append(Markup.Block(p.Text)).Append("</p>\n");
                    break;
                }

                case ListBlock l:
                {
                    var tag = l.Ordered ? "ol" : "ul";
                    sb.Append('<').Append(tag).Append(" class=\"list\"").Append(sp).Append(">\n");
                    foreach (var item in l.Items)
                        sb.Append("  <li>").Append(Markup.Inline(item)).Append("</li>\n");
                    sb.Append("</").Append(tag).Append(">\n");
                    break;
                }

                case ChecklistBlock c:
                {
                    sb.Append("<div class=\"checklist\"").Append(sp).Append(">\n");
                    if (!string.IsNullOrWhiteSpace(c.Title))
                        sb.Append("  <div class=\"check-title\">").Append(Markup.Inline(c.Title)).Append("</div>\n");
                    sb.Append("  <div class=\"check-grid\" style=\"grid-template-columns: repeat(")
                      .Append(Math.Clamp(c.Columns, 1, 3)).Append(", 1fr)\">\n");
                    foreach (var item in c.Items)
                        sb.Append("    <label class=\"check-item\"><span class=\"box\"></span><span>")
                          .Append(Markup.Inline(item)).Append("</span></label>\n");
                    sb.Append("  </div>\n</div>\n");
                    break;
                }

                case TableBlock t:
                {
                    sb.Append("<table class=\"tbl tbl-").Append(t.Style).Append('"').Append(sp).Append(">\n");
                    if (t.Headers is { Count: > 0 } && t.HeaderRow)
                    {
                        sb.Append("  <thead><tr>");
                        foreach (var head in t.Headers)
                            sb.Append("<th>").Append(Markup.Inline(head)).Append("</th>");
                        sb.Append("</tr></thead>\n");
                    }
                    sb.Append("  <tbody>\n");
                    if (t.Headers is { Count: > 0 } && !t.HeaderRow)
                        AppendTableRow(sb, t.Headers, null);
                    var rowH = t.RowHeightPt is > 0 ? $" style=\"height:{N(t.RowHeightPt.Value)}pt\"" : "";
                    foreach (var row in t.Rows)
                        AppendTableRow(sb, row, rowH);
                    sb.Append("  </tbody>\n</table>\n");
                    break;
                }

                case RuledLinesBlock rl:
                {
                    sb.Append("<div class=\"lines\"").Append(sp).Append(">\n");
                    if (!string.IsNullOrWhiteSpace(rl.Label))
                        sb.Append("  <div class=\"lines-label\">").Append(Markup.Inline(rl.Label)).Append("</div>\n");
                    var line = $"<div class=\"line\" style=\"height:{N(rl.GapPt)}pt\"></div>\n";
                    for (var i = 0; i < rl.Count; i++) sb.Append("  ").Append(line);
                    sb.Append("</div>\n");
                    break;
                }

                case PromptBlock pr:
                {
                    sb.Append("<div class=\"prompt\"").Append(sp).Append(">\n  <div class=\"prompt-q\">");
                    if (pr.Numbered)
                    {
                        _promptCounter++;
                        sb.Append("<span class=\"prompt-num\">").Append(_promptCounter).Append(".</span>");
                    }
                    sb.Append(Markup.Inline(pr.Text));
                    if (!string.IsNullOrWhiteSpace(pr.Hint))
                        sb.Append("<span class=\"prompt-hint\">").Append(Markup.Inline(pr.Hint)).Append("</span>");
                    sb.Append("</div>\n");
                    for (var i = 0; i < pr.Lines; i++)
                        sb.Append("  <div class=\"line\" style=\"height:26pt\"></div>\n");
                    sb.Append("</div>\n");
                    break;
                }

                case QuoteBlock q:
                    sb.Append("<blockquote class=\"quote\"").Append(sp).Append("><p>")
                      .Append(Markup.Block(q.Text)).Append("</p>");
                    if (!string.IsNullOrWhiteSpace(q.Attribution))
                        sb.Append("<cite>").Append(Markup.Inline(q.Attribution)).Append("</cite>");
                    sb.Append("</blockquote>\n");
                    break;

                case CalloutBlock co:
                {
                    var defaultTitle = co.Style switch { "tip" => "Tip", "warning" => "Important", _ => "Note" };
                    sb.Append("<div class=\"callout ").Append(co.Style).Append('"').Append(sp).Append(">\n");
                    sb.Append("  <div class=\"callout-title\">").Append(Markup.Inline(co.Title ?? defaultTitle)).Append("</div>\n");
                    sb.Append("  <div class=\"callout-text\">").Append(Markup.Block(co.Text)).Append("</div>\n");
                    sb.Append("</div>\n");
                    break;
                }

                case DividerBlock d:
                    sb.Append(d.Style == "dots" ? "<hr class=\"dots\">\n" : "<hr class=\"rule\">\n");
                    break;

                case SpacerBlock s2:
                    sb.Append("<div class=\"spacer\" style=\"height:").Append(N(s2.HeightPt)).Append("pt\"></div>\n");
                    break;

                case PageBreakBlock:
                    sb.Append("<div class=\"brk\"></div>\n");
                    break;

                case ImageBlock img:
                    if (!TryResolveImage(img.Src, path, out var src, out _)) break;
                    var width = img.WidthIn is > 0 ? $" style=\"width:{N(img.WidthIn.Value)}in\"" : "";
                    sb.Append("<img class=\"block-img img-").Append(img.Align).Append('"').Append(width)
                      .Append(" src=\"").Append(src).Append("\" alt=\"")
                      .Append(Markup.Plain(img.Alt ?? "")).Append("\">\n");
                    break;

                case RepeatBlock rep:
                {
                    if (depth >= 3) break; // safety net; validator rejects deep nesting anyway
                    for (var n = 1; n <= rep.Times; n++)
                    {
                        var brk = n < rep.Times ? " brk" : "";
                        sb.Append("<div class=\"iteration").Append(brk).Append('"').Append(sp).Append(">\n");
                        if (!string.IsNullOrWhiteSpace(rep.Label))
                        {
                            var label = rep.Label
                                .Replace("{n}", n.ToString(Inv), StringComparison.OrdinalIgnoreCase)
                                .Replace("{total}", rep.Times.ToString(Inv), StringComparison.OrdinalIgnoreCase);
                            sb.Append("  <div class=\"iter-label\">").Append(Markup.Inline(label)).Append("</div>\n");
                        }
                        AppendBlocks(sb, rep.Blocks, $"{path}.blocks", depth + 1);
                        sb.Append("</div>\n");
                    }
                    break;
                }

                default:
                    break;
            }
        }

        private static void AppendTableRow(StringBuilder sb, IReadOnlyList<string> cells, string? rowStyle)
        {
            sb.Append("    <tr").Append(rowStyle).Append('>');
            foreach (var cell in cells)
                sb.Append("<td>").Append(Markup.Inline(cell)).Append("</td>");
            sb.Append("</tr>\n");
        }

        private static string AlignClass(string? align)
            => align?.ToLowerInvariant() switch
            {
                "center" => " align-center",
                "right" => " align-right",
                "left" => " align-left",
                _ => "",
            };

        private static string SpacingStyle(WorkbookBlock b)
        {
            var parts = new List<string>(2);
            if (b.SpacingBeforePt is > 0) parts.Add($"margin-top:{N(b.SpacingBeforePt.Value)}pt");
            if (b.SpacingAfterPt is > 0) parts.Add($"margin-bottom:{N(b.SpacingAfterPt.Value)}pt");
            return parts.Count == 0 ? "" : $" style=\"{string.Join("; ", parts)}\"";
        }

        /// <summary>Resolves an image reference to something Chromium can load offline (data URI).</summary>
        private bool TryResolveImage(string src, string path, out string resolved, out string? warning)
        {
            resolved = "";
            warning = null;

            if (string.IsNullOrWhiteSpace(src))
            {
                warning = "image src is empty";
                _warnings.Add(new ValidationIssue(path, "Image src is empty.", IsWarning: true));
                return false;
            }

            if (src.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                resolved = src;
                return true;
            }

            var isRemote = false;
            if (Uri.TryCreate(src, UriKind.Absolute, out var uri) && !uri.IsFile)
            {
                resolved = src;
                isRemote = uri.Scheme is "http" or "https";
            }
            else
            {
                var full = Path.GetFullPath(uri is { IsFile: true } ? uri.LocalPath : Path.Combine(_ctx.BasePath, src));
                if (!File.Exists(full))
                {
                    warning = "image file not found";
                    _warnings.Add(new ValidationIssue(path, $"Image file not found: {src}", IsWarning: true));
                    return false;
                }
                try
                {
                    var bytes = File.ReadAllBytes(full);
                    resolved = $"data:{MimeFor(Path.GetExtension(full))};base64,{Convert.ToBase64String(bytes)}";
                }
                catch (Exception ex)
                {
                    warning = "failed to read image";
                    _warnings.Add(new ValidationIssue(path, $"Failed to read image '{src}': {ex.Message}", IsWarning: true));
                    return false;
                }
            }

            if (isRemote)
            {
                warning = "remote image (requires network access while rendering)";
                _warnings.Add(new ValidationIssue(path, warning + ": " + src, IsWarning: true));
            }
            return true;
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
}
