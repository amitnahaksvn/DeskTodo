#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds a self-contained win-x64 publish of DeskTodo and packs it into an
    unsigned .msix using makeappx.exe.

.DESCRIPTION
    AUTHORED, NOT VERIFIED: this repo's dev environment is macOS-only, so
    this script has never actually been run. makeappx.exe/signtool.exe ship
    with the Windows 10/11 SDK — install via Visual Studio or the standalone
    SDK installer, then run this from a "Developer PowerShell" prompt so
    they're on PATH. See docs/ARCHITECTURE.md's "Phase 16" section.

    Known gap: packaging/windows/AppxManifest.xml references four logo PNGs
    (Assets\StoreLogo.png, Square150x150Logo.png, Square44x44Logo.png,
    Wide310x150Logo.png) that don't exist yet — this repo only has the
    placeholder avalonia-logo.ico, not real DeskTodo branding at the sizes
    MSIX requires. makeappx will fail until those are supplied; this script
    stops early with a clear error instead of producing a manifest-invalid
    package.

.PARAMETER Version
    Four-part version to stamp into the manifest (MSIX requires exactly
    Major.Minor.Build.Revision). Defaults to 1.0.0.0.
#>
param(
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$AppCsproj = Join-Path $RepoRoot "src\DeskTodo.App\DeskTodo.App.csproj"
$BuildDir = Join-Path $RepoRoot "artifacts\windows"
$PublishDir = Join-Path $BuildDir "publish-win-x64"
$PackageStagingDir = Join-Path $BuildDir "package-staging"
$MsixPath = Join-Path $BuildDir "DeskTodo-$Version-win-x64.msix"

$RequiredAssets = @("StoreLogo.png", "Square150x150Logo.png", "Square44x44Logo.png", "Wide310x150Logo.png")
$AssetsSourceDir = Join-Path $RepoRoot "packaging\windows\Assets"
$MissingAssets = $RequiredAssets | Where-Object { -not (Test-Path (Join-Path $AssetsSourceDir $_)) }
if ($MissingAssets.Count -gt 0) {
    Write-Error "Missing MSIX logo asset(s) in packaging\windows\Assets: $($MissingAssets -join ', '). Add real DeskTodo branding at the sizes MSIX requires before packaging — see this script's doc comment."
}

Write-Host "==> Publishing self-contained (win-x64)..."
if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }
dotnet publish $AppCsproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o $PublishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Write-Host "==> Staging package contents..."
if (Test-Path $PackageStagingDir) { Remove-Item -Recurse -Force $PackageStagingDir }
New-Item -ItemType Directory -Path $PackageStagingDir | Out-Null
Copy-Item -Recurse "$PublishDir\*" $PackageStagingDir
New-Item -ItemType Directory -Path (Join-Path $PackageStagingDir "Assets") | Out-Null
Copy-Item "$AssetsSourceDir\*" (Join-Path $PackageStagingDir "Assets")

$ManifestContent = Get-Content (Join-Path $RepoRoot "packaging\windows\AppxManifest.xml") -Raw
$ManifestContent = $ManifestContent -replace 'Version="1\.0\.0\.0"', "Version=`"$Version`""
Set-Content -Path (Join-Path $PackageStagingDir "AppxManifest.xml") -Value $ManifestContent -NoNewline

Write-Host "==> Packing .msix..."
if (Test-Path $MsixPath) { Remove-Item -Force $MsixPath }
& makeappx.exe pack /d $PackageStagingDir /p $MsixPath
if ($LASTEXITCODE -ne 0) { throw "makeappx.exe failed" }

Write-Host "==> Done: $MsixPath"
Write-Host "==> Unsigned — Windows will refuse to install this until it's signed with a trusted certificate:"
Write-Host "    signtool.exe sign /fd SHA256 /a /f <your.pfx> /p <password> `"$MsixPath`""
