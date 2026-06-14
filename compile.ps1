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
$counterStrikeSharpTarget = Join-Path $compiledRoot 'counterstrikesharp'
$defaultSharpModMenuRoot = 'C:\Users\micka\Documents\GitHub\SharpModMenu'
$sharpModMenuRoot = if ($env:SHARPMODMENU_ROOT) { $env:SHARPMODMENU_ROOT } else { $defaultSharpModMenuRoot }
$sharpModMenuCompiledRoot = Join-Path $sharpModMenuRoot 'compiled/counterstrikesharp'

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

if (Test-Path $sharpModMenuCompiledRoot) {
    foreach ($relativePath in @(
        'plugins/SharpModMenu',
        'shared/SharpModMenu',
        'shared/CSSUniversalMenuAPI',
        'configs/plugins/SharpModMenu'
    )) {
        $sourcePath = Join-Path $sharpModMenuCompiledRoot $relativePath
        if (-not (Test-Path $sourcePath)) {
            continue
        }

        $targetPath = Join-Path $counterStrikeSharpTarget $relativePath
        New-Item -ItemType Directory -Path (Split-Path -Parent $targetPath) -Force | Out-Null
        Copy-Item -Path $sourcePath -Destination (Split-Path -Parent $targetPath) -Recurse -Force
        Write-Host " - SharpModMenu component copied to: $targetPath"
    }

    $sharpModMenuConfig = Join-Path $counterStrikeSharpTarget 'configs/plugins/SharpModMenu/sharpmodmenu_config.jsonc'
    if (Test-Path $sharpModMenuConfig) {
        $configText = Get-Content -Raw -Path $sharpModMenuConfig
        $configText = $configText.Replace(
            "<font color='#D10D0D'>Select: </font><font color='#F2A10F'>ZS/Use</font> <font color='#FFFFFF'>|</font> <font color='#D10D0D'>Exit:</font> <font color='#F2A10F'>Reload</font>",
            "<font color='#D10D0D'>Select: </font><font color='#F2A10F'>ZS/Use</font>"
        )
        Set-Content -Path $sharpModMenuConfig -Value $configText -NoNewline
        Write-Host " - SharpModMenu compact footer hides Exit text while keeping Reload close support."
    }
} else {
    Write-Warning "SharpModMenu compiled output not found at $sharpModMenuCompiledRoot. Build SharpModMenu first or set SHARPMODMENU_ROOT."
}

# Zip the staged plugin + shared folder for convenience
$zipPath = Join-Path $compiledRoot "$pluginName.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
Compress-Archive -Path (Join-Path $compiledRoot 'counterstrikesharp/*') -DestinationPath $zipPath

Write-Host "[OK] Build finished."
Write-Host " - Folder: $pluginTarget"
Write-Host " - SharpModMenu source: $sharpModMenuCompiledRoot"
Write-Host " - Zip:    $zipPath"
