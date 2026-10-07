using System.Globalization;
using System.Text;
using PdfKits.Core.Model;

namespace PdfKits.Core.Rendering;

/// <summary>
/// Builds the embedded stylesheet for a workbook. All sizing is done in points/inches so
/// output is identical in screen preview and in both Chromium-based PDF engines.
/// </summary>
public static class WorkbookCss
{
    private static string N(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    public static string Build(Workbook wb)
    {
        var t = wb.Theme;
        var (_, contentH) = PageGeometry.ContentInches(wb.Page);
        // Small buffer keeps full-height cover blocks from spilling onto an extra page.
        var coverH = N(Math.Max(3, contentH - 0.06));
        var copyrightPad = N(contentH * 0.45);

        var sb = new StringBuilder();
        void W(string line) => sb.AppendLine(line);

        W("*, *::before, *::after { box-sizing: border-box; }");
        W("html, body { margin: 0; padding: 0; }");
        W($"body {{ font-family: {t.BodyFont}; font-size: {N(t.BaseFontSizePt)}pt; line-height: {N(t.LineHeight)}; color: {t.InkColor}; -webkit-print-color-adjust: exact; print-color-adjust: exact; }}");
        W("p { margin: 0 0 8pt; }");
        W("strong, b { font-weight: 600; }");
        W("em, i { font-style: italic; }");
        W("");

        W(".brk { page-break-after: always; break-after: page; }");
        W("");

        // Cover ----------------------------------------------------------------
        W(".cover { page-break-after: always; min-height: " + coverH + "in; display: flex; flex-direction: column; }");
        W($"  .cover-band {{ height: 0.5in; background: {t.PrimaryColor}; }}");
        W($"  .cover-band.bottom {{ height: 0.16in; background: {t.SecondaryColor}; }}");
        W("  .cover-body { flex: 1 1 auto; padding-top: 0.6in; }");
        W($".cover-kicker {{ font-size: 9.5pt; letter-spacing: 0.34em; text-transform: uppercase; color: {t.SecondaryColor}; font-weight: 700; margin-bottom: 0.32in; }}");
        W($".cover-title {{ font-family: {t.HeadingFont}; font-size: 33pt; line-height: 1.12; color: {t.PrimaryColor}; margin: 0; font-weight: 600; }}");
        W($".cover-rule {{ width: 1.7in; height: 3pt; background: {t.AccentColor}; margin: 16pt 0 16pt; }}");
        W($".cover-subtitle {{ font-size: 15pt; line-height: 1.35; color: {t.InkColor}; }}");
        W($".cover-tagline {{ margin-top: 10pt; font-size: 11pt; color: {t.MutedColor}; font-style: italic; }}");
        W(".cover-foot { padding: 0 0 14pt; display: flex; justify-content: space-between; align-items: flex-end; }");
        W($".cover-author {{ font-size: 11.5pt; letter-spacing: 0.2em; text-transform: uppercase; color: {t.PrimaryColor}; font-weight: 600; }}");
        W($".cover-edition {{ font-size: 8.5pt; color: {t.MutedColor}; letter-spacing: 0.12em; text-transform: uppercase; }}");
        W(".cover-logo { max-height: 0.85in; max-width: 3in; margin-bottom: 0.24in; }");
        W("");

        // Front matter ---------------------------------------------------------
        W($".front-title {{ font-family: {t.HeadingFont}; font-size: 21pt; color: {t.PrimaryColor}; margin: 0 0 6pt; font-weight: 600; }}");
        W($"  .front-rule {{ border: none; border-top: 2.5pt solid {t.PrimaryColor}; margin: 0 0 16pt; }}");
        W($".copyright {{ font-size: 8.5pt; color: {t.MutedColor}; line-height: 1.65; padding-top: {copyrightPad}in; }}");
        W($".copyright .cr-title {{ font-family: {t.HeadingFont}; font-size: 13pt; color: {t.PrimaryColor}; margin-bottom: 8pt; font-weight: 600; }}");
        W(".copyright p { margin: 0 0 7pt; }");
        W(".toc-list { list-style: none; margin: 0; padding: 0; }");
        W(".toc-item { display: flex; align-items: baseline; padding: 7.5pt 0; }");
        W($".toc-num {{ width: 0.6in; font-family: {t.HeadingFont}; font-size: 11pt; color: {t.SecondaryColor}; font-weight: 700; }}");
        W($".toc-text {{ font-family: {t.HeadingFont}; font-size: 12.5pt; color: {t.PrimaryColor}; flex: 1 1 auto; border-bottom: 1px dotted {t.LineColor}; padding-bottom: 4pt; margin-right: 10pt; }}");
        W($".toc-sub {{ font-size: 8.5pt; color: {t.MutedColor}; font-style: italic; padding-bottom: 4pt; border-bottom: 1px dotted {t.LineColor}; }}");
        W("");

        // Sections -------------------------------------------------------------
        W(".chapter { page-break-after: always; }");
        W(".chapter.last, .chapter.cont { page-break-after: auto; }");
        W(".chapter-head { margin-bottom: 14pt; }");
        W($".chapter-num {{ font-size: 8pt; letter-spacing: 0.3em; text-transform: uppercase; color: {t.SecondaryColor}; font-weight: 700; margin-bottom: 5pt; }}");
        W($".chapter-title {{ font-family: {t.HeadingFont}; font-size: 22pt; color: {t.PrimaryColor}; margin: 0; line-height: 1.15; font-weight: 600; }}");
        W($".chapter-sub {{ font-size: 10.5pt; color: {t.MutedColor}; font-style: italic; margin-top: 4pt; }}");
        W($".chapter-rule {{ border: none; border-top: 2.5pt solid {t.AccentColor}; width: 2.2in; margin: 10pt 0 0; }}");
        W("");

        // Typography -----------------------------------------------------------
        W($".h1 {{ font-family: {t.HeadingFont}; font-size: 18pt; color: {t.PrimaryColor}; margin: 14pt 0 6pt; font-weight: 600; }}");
        W($".h2 {{ font-family: {t.HeadingFont}; font-size: 14.5pt; color: {t.PrimaryColor}; margin: 13pt 0 5pt; font-weight: 600; }}");
        W($".h3 {{ font-family: {t.HeadingFont}; font-size: 11.5pt; color: {t.PrimaryColor}; margin: 11pt 0 4pt; font-weight: 600; letter-spacing: 0.05em; }}");
        W(".lead { font-size: 12pt; line-height: 1.6; }");
        W($"  .small {{ font-size: 8.5pt; line-height: 1.55; color: {t.MutedColor}; }}");
        W(".align-left { text-align: left; } .align-center { text-align: center; } .align-right { text-align: right; }");
        W("ul.list, ol.list { margin: 4pt 0 10pt; padding-left: 20pt; }");
        W("ul.list li, ol.list li { margin-bottom: 4.5pt; }");
        W("");

        // Checklist ------------------------------------------------------------
        W(".checklist { margin: 6pt 0 12pt; page-break-inside: avoid; }");
        W($"  .check-title {{ font-size: 9.5pt; font-weight: 700; color: {t.PrimaryColor}; margin-bottom: 7pt; text-transform: uppercase; letter-spacing: 0.1em; }}");
        W(".check-grid { display: grid; gap: 7pt 16pt; }");
        W(".check-item { display: flex; align-items: flex-start; gap: 7pt; font-size: 10pt; }");
        W($"  .box {{ flex: 0 0 auto; width: 11pt; height: 11pt; border: 1.2pt solid {t.PrimaryColor}; margin-top: 2.5pt; border-radius: 1.5pt; background: #FFFFFF; }}");
        W("");

        // Tables ---------------------------------------------------------------
        W("table.tbl { width: 100%; border-collapse: collapse; margin: 6pt 0 12pt; font-size: 9.5pt; }");
        W("  table.tbl thead { display: table-header-group; }");
        W("  table.tbl tr { page-break-inside: avoid; }");
        W($"  .tbl th {{ background: {t.PrimaryColor}; color: #FFFFFF; font-weight: 600; text-align: left; padding: 5pt 6pt; letter-spacing: 0.03em; }}");
        W("  .tbl td { padding: 5pt 6pt; vertical-align: top; }");
        W($"  .tbl-grid td {{ border: 1px solid {t.LineColor}; }}");
        W($"  .tbl-ruled td {{ border-bottom: 1px solid {t.LineColor}; }}");
        W("  .tbl-plain td { border: none; padding-left: 0; }");
        W("");

        // Writing surfaces -----------------------------------------------------
        W(".lines { margin: 4pt 0 8pt; }");
        W($"  .lines-label {{ font-size: 8pt; color: {t.MutedColor}; text-transform: uppercase; letter-spacing: 0.16em; margin-bottom: 4pt; font-weight: 600; }}");
        W($"  .line {{ border-bottom: 1px solid {t.LineColor}; }}");
        W(".prompt { margin: 9pt 0 13pt; page-break-inside: avoid; }");
        W("  .prompt-q { font-size: 11pt; font-weight: 600; margin-bottom: 5pt; }");
        W($"  .prompt-num {{ color: {t.SecondaryColor}; font-weight: 700; margin-right: 5pt; }}");
        W($"  .prompt-hint {{ display: block; font-size: 8.5pt; font-style: italic; color: {t.MutedColor}; font-weight: 400; margin-top: 3pt; }}");
        W("");

        // Quote / callout / dividers ------------------------------------------
        W($".quote {{ margin: 10pt 0; padding: 6pt 0 6pt 14pt; border-left: 3pt solid {t.SecondaryColor}; font-family: {t.HeadingFont}; font-size: 13pt; font-style: italic; color: {t.PrimaryColor}; page-break-inside: avoid; }}");
        W($"  .quote cite {{ display: block; margin-top: 6pt; font-family: {t.BodyFont}; font-size: 8.5pt; font-style: normal; color: {t.MutedColor}; letter-spacing: 0.08em; text-transform: uppercase; }}");
        W($".callout {{ border-left: 4pt solid {t.PrimaryColor}; background: {t.SurfaceColor}; padding: 8pt 10pt; margin: 8pt 0 12pt; page-break-inside: avoid; }}");
        W($"  .callout.tip {{ border-left-color: {t.AccentColor}; }}");
        W($"  .callout.warning {{ border-left-color: {t.SecondaryColor}; }}");
        W($"  .callout-title {{ font-weight: 700; font-size: 8.5pt; text-transform: uppercase; letter-spacing: 0.12em; color: {t.PrimaryColor}; margin-bottom: 3pt; }}");
        W($"  .callout.tip .callout-title {{ color: {t.AccentColor}; }}");
        W($"  .callout.warning .callout-title {{ color: {t.PrimaryColor}; }}");
        W($"hr.rule {{ border: none; border-top: 1px solid {t.LineColor}; margin: 10pt 0; }}");
        W($"hr.dots {{ border: none; border-top: 2px dotted {t.LineColor}; margin: 10pt 0; }}");
        W("");

        // Images / repeats -----------------------------------------------------
        W("img.block-img { display: block; }");
        W("  .img-left { margin: 6pt 0; } .img-center { margin: 6pt auto; } .img-right { margin: 6pt 0 6pt auto; }");
        W(".iteration.brk { page-break-after: always; break-after: page; }");
        W($"  .iter-label {{ font-size: 8.5pt; text-transform: uppercase; letter-spacing: 0.2em; color: {t.SecondaryColor}; font-weight: 700; margin-bottom: 8pt; }}");
        W(".spacer { display: block; }");

        return sb.ToString();
    }
}
