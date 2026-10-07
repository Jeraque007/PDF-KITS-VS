$root = "F:\Jeraque\PDF KITS VS"
Set-Location $root
git init -q
git config user.name "PdfKits Bot"
git config user.email "bot@pdfkits.local"

$msg = @"
Initial commit: PdfKits - sellable PDF workbook generator from JSON

Core model, JSON serialization/validation, HTML renderer (inline markup,
data URI images, header/footer fixtures), dual PDF engines (DynamicPDF
evaluation + IronPDF commercial), CLI (generate/validate/init/help), and a
24-test xUnit suite. Sample 13-week workbook included.

Co-authored-by: Solar Mini4 <solar-mini4@upstage.ai>
"@

$msg | Set-Content -Path (Join-Path $env:TEMP "pdfkits-commit-msg.txt") -Encoding UTF8
git add -A
git commit -F (Join-Path $env:TEMP "pdfkits-commit-msg.txt") -q
git --no-pager log --oneline -2
Write-Host "--- status ---"
git status --short
Write-Host "(clean)"
