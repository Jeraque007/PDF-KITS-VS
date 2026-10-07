$root = "F:\Jeraque\PDF KITS VS"
Set-Location $root
git init -q
git config user.name "PdfKits Bot"
git config user.email "bot@pdfkits.local"

$msg = @"
Deployment: image-generation request handling, license-key removal, and Chrome headless PDF engine.

Core model now exposes ImageInstruction + IImageGenerator so workbook
image-generation requests are honoured (or explicitly warned) instead of
silently dropped. License-key members removed from IPdfEngine and all
engines; DynamicPDF/IronPDF no longer read env keys. ChromeHeadlessPdfEngine
ships headless print-to-PDF with a HOAWS running footer (date + page number).
README, CLI help, and PdfEngines factory updated accordingly (auto now
prefers Chrome when installed).

Co-authored-by: Solar Mini4 <solar-mini4@upstage.ai>
"@

$msg | Set-Content -Path (Join-Path $env:TEMP "pdfkits-commit-msg.txt") -Encoding UTF8
git add -A
git commit -F (Join-Path $env:TEMP "pdfkits-commit-msg.txt") -q
git --no-pager log --oneline -2
Write-Host "--- status ---"
git status --short
Write-Host "(clean)"
