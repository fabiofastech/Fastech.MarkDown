<#
.SYNOPSIS
    Build script per l'MSI di Fastech Markdown Viewer.

.DESCRIPTION
    1. Verifica / installa WiX v5 come dotnet global tool
    2. Pubblica l'app (self-contained, win-x64) via dotnet publish
    3. Genera _AppFiles.wxs con la struttura file/directory completa
    4. Compila l'MSI con wix build
    5. Pulisce i file temporanei

.PARAMETER Version
    Versione da incorporare nell'MSI (default: 1.0.0.0)

.PARAMETER OutputDir
    Cartella di destinazione dell'MSI (default: ..\output)

.EXAMPLE
    .\build-installer.ps1
    .\build-installer.ps1 -Version 1.2.0.0 -OutputDir C:\dist
#>
param(
    [string]$Version   = "1.0.0.0",
    [string]$OutputDir = (Join-Path $PSScriptRoot "..\output")
)

$ErrorActionPreference = "Stop"

$repoRoot   = (Resolve-Path "$PSScriptRoot\..")
$appProject = "$repoRoot\Fastech.MarkDown.App\Fastech.MarkDown.App.csproj"
$publishDir = "$PSScriptRoot\_publish"
$tmpWxs     = "$PSScriptRoot\_AppFiles.wxs"

function Write-Step([string]$msg) { Write-Host "  $msg" -ForegroundColor Cyan }
function Write-Ok([string]$msg)   { Write-Host "  $msg" -ForegroundColor Green }

Write-Host ""
Write-Host "=== Fastech Markdown Viewer — Build Installer ===" -ForegroundColor White
Write-Host "    Versione : $Version"
Write-Host "    Output   : $OutputDir"
Write-Host ""

# ── 1. Verifica / installa WiX ────────────────────────────────────────────────
Write-Step "Verifica WiX toolset..."

if (-not (Get-Command wix -ErrorAction SilentlyContinue)) {
    Write-Step "WiX non trovato — installazione in corso..."
    dotnet tool install --global wix
    if ($LASTEXITCODE -ne 0) { throw "Installazione WiX fallita." }

    # Aggiorna PATH nella sessione corrente
    $toolsPath = "$env:USERPROFILE\.dotnet\tools"
    if ($env:PATH -notlike "*$toolsPath*") {
        $env:PATH = "$toolsPath;$env:PATH"
    }
    Write-Ok "WiX installato."
}
else {
    Write-Ok "WiX trovato: $(wix --version 2>&1 | Select-Object -First 1)"
}

# ── 2. Pubblica l'app (self-contained win-x64) ────────────────────────────────
Write-Step "Pubblicazione app (Release, win-x64, self-contained)..."

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

dotnet publish $appProject `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o $publishDir `
    /p:Version=$Version `
    /p:PublishReadyToRun=true `
    --nologo -v minimal

if ($LASTEXITCODE -ne 0) { throw "dotnet publish fallito." }
Write-Ok "App pubblicata in: $publishDir"

# ── 3. Genera _AppFiles.wxs dalla directory pubblicata ───────────────────────
Write-Step "Generazione _AppFiles.wxs..."

$allFiles = Get-ChildItem -Path $publishDir -Recurse -File
$dirs     = [ordered]@{}   # relPath -> @{Id; Name; ParentId}
$comps    = [System.Collections.Generic.List[hashtable]]::new()
$dirIdx   = 0
$compIdx  = 0

function Ensure-Dir([string]$relPath) {
    if (-not $relPath -or $dirs.Contains($relPath)) { return }
    $parent = Split-Path $relPath -Parent
    if ($parent) { Ensure-Dir $parent }
    $script:dirIdx++
    $dirs[$relPath] = @{
        Id       = "Dir$($script:dirIdx)"
        Name     = (Split-Path $relPath -Leaf)
        ParentId = if ($parent) { $dirs[$parent].Id } else { 'INSTALLFOLDER' }
    }
}

foreach ($f in $allFiles) {
    $rel    = $f.FullName.Substring($publishDir.Length).TrimStart('\', '/')
    $relDir = Split-Path $rel -Parent
    if ($relDir) { Ensure-Dir $relDir }

    $script:compIdx++
    $comps.Add(@{
        Id    = "C$compIdx"
        FId   = "F$compIdx"
        Src   = $f.FullName
        DirId = if ($relDir) { $dirs[$relDir].Id } else { 'INSTALLFOLDER' }
    })
}

$xml = [System.Text.StringBuilder]::new()
$null = $xml.AppendLine('<?xml version="1.0" encoding="UTF-8"?>')
$null = $xml.AppendLine('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
$null = $xml.AppendLine('  <Fragment>')

# Directory tree
if ($dirs.Count -gt 0) {
    $null = $xml.AppendLine('    <DirectoryRef Id="INSTALLFOLDER">')

    function Write-DirNode([string]$parentId) {
        foreach ($kv in $dirs.GetEnumerator() | Where-Object { $_.Value.ParentId -eq $parentId }) {
            $d = $kv.Value
            $null = $xml.AppendLine("      <Directory Id=""$($d.Id)"" Name=""$($d.Name)"">")
            Write-DirNode $d.Id
            $null = $xml.AppendLine("      </Directory>")
        }
    }
    Write-DirNode 'INSTALLFOLDER'
    $null = $xml.AppendLine('    </DirectoryRef>')
}

# ComponentGroup
$null = $xml.AppendLine('    <ComponentGroup Id="AppFiles">')
foreach ($c in $comps) {
    $null = $xml.AppendLine("      <Component Id=""$($c.Id)"" Directory=""$($c.DirId)"" Guid=""*"">")
    $null = $xml.AppendLine("        <File Id=""$($c.FId)"" Source=""$($c.Src)"" KeyPath=""yes"" />")
    $null = $xml.AppendLine("      </Component>")
}
$null = $xml.AppendLine('    </ComponentGroup>')
$null = $xml.AppendLine('  </Fragment>')
$null = $xml.AppendLine('</Wix>')

[System.IO.File]::WriteAllText($tmpWxs, $xml.ToString(), [System.Text.Encoding]::UTF8)
Write-Ok "$($comps.Count) file, $($dirs.Count) directory."

# ── 4. Compila l'MSI ─────────────────────────────────────────────────────────
Write-Step "Compilazione MSI..."

if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir | Out-Null }

$msiPath = Join-Path $OutputDir "FastechMarkdownViewer-$Version.msi"

wix build "$PSScriptRoot\Package.wxs" $tmpWxs -o $msiPath -arch x64 -acceptEula wix7 -d ProductVersion=$Version

if ($LASTEXITCODE -ne 0) { throw "wix build fallito." }

# ── 5. Cleanup ────────────────────────────────────────────────────────────────
Write-Step "Pulizia file temporanei..."
Remove-Item $tmpWxs    -Force              -ErrorAction SilentlyContinue
Remove-Item $publishDir -Recurse -Force    -ErrorAction SilentlyContinue

Write-Host ""
Write-Ok "MSI pronto: $msiPath"
Write-Host ""
