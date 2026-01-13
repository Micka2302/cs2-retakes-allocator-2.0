#!/usr/bin/env pwsh
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$solution = Join-Path $root 'cs2-retakes-allocator.sln'
$buildOutput = Join-Path $root 'RetakesAllocator/bin/Release/net8.0'
$compiledRoot = Join-Path $root 'compiled'
$pluginName = 'RetakesAllocator'
$pluginTarget = Join-Path $compiledRoot "counterstrikesharp/plugins/$pluginName"

# Clean staging directory
Remove-Item -Recurse -Force $compiledRoot -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $pluginTarget -Force | Out-Null

dotnet restore $solution
dotnet build $solution -c Release --no-restore --nologo

if (-not (Test-Path $buildOutput)) {
    throw "Build output not found at $buildOutput"
}

# Stage plugin files
Copy-Item -Path (Join-Path $buildOutput '*') -Destination $pluginTarget -Recurse -Force

# Keep only linux and Windows runtimes to mirror release packaging
$runtimeDir = Join-Path $pluginTarget 'runtimes'
if (Test-Path $runtimeDir) {
    $keep = @('linux-x64', 'win-x64')
    Get-ChildItem $runtimeDir -Directory | Where-Object { $keep -notcontains $_.Name } | Remove-Item -Recurse -Force
} else {
    Write-Host '[WARN] No runtimes directory found in build output.'
}

# Strip CSS API (already provided by server)
$cssApi = Join-Path $pluginTarget 'CounterStrikeSharp.API.dll'
if (Test-Path $cssApi) {
    Remove-Item $cssApi -Force
}

# Zip the staged plugin for convenience
$zipPath = Join-Path $compiledRoot "$pluginName.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
Compress-Archive -Path (Join-Path $pluginTarget '*') -DestinationPath $zipPath

Write-Host "[OK] Build finished."
Write-Host " - Folder: $pluginTarget"
Write-Host " - Zip:    $zipPath"
