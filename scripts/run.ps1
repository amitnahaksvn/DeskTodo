#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs DeskTodo locally from source (Windows dev loop) — a thin wrapper
    around `dotnet run` so contributors don't need to remember the project
    path.

.DESCRIPTION
    AUTHORED, NOT VERIFIED: this repo's dev environment is macOS-only, so
    this script has never actually been run — see scripts/package-windows.ps1
    for the same caveat on the Windows packaging script.

.PARAMETER AppArgs
    Extra arguments passed straight through to the app.
#>
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$AppArgs
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir
$AppCsproj = Join-Path $RepoRoot "src/DeskTodo.App/DeskTodo.App.csproj"

dotnet run --project $AppCsproj @AppArgs
