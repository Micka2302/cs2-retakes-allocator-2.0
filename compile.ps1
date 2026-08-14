#!/usr/bin/env pwsh
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$solution = Join-Path $root 'cs2-retakes-allocator.sln'
$buildOutput = Join-Path $root 'RetakesAllocator/bin/Release/net10.0'
$compiledRoot = Join-Path $root 'compiled'
$pluginName = 'RetakesAllocator'
$pluginTarget = Join-Path $compiledRoot "counterstrikesharp/plugins/$pluginName"
$counterStrikeSharpTarget = Join-Path $compiledRoot 'counterstrikesharp'
$defaultAbsynthiumMenuRoot = Join-Path (Split-Path -Parent $root) 'Absynthium_Menu'
$absynthiumMenuRoot = if ($env:ABSYNTHIUM_MENU_ROOT) { $env:ABSYNTHIUM_MENU_ROOT } else { $defaultAbsynthiumMenuRoot }
$absynthiumMenuCompiledRoot = Join-Path $absynthiumMenuRoot 'compiled/counterstrikesharp'
$dependencyCache = Join-Path $root 'obj/dependencies'

function Get-VerifiedPackage {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Url,
        [Parameter(Mandatory = $true)][string]$Sha256
    )

    New-Item -ItemType Directory -Path $dependencyCache -Force | Out-Null
    $packagePath = Join-Path $dependencyCache $Name
    $needsDownload = -not (Test-Path -LiteralPath $packagePath -PathType Leaf)

    if (-not $needsDownload) {
        $actualHash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash
        $needsDownload = $actualHash -ne $Sha256
    }

    if ($needsDownload) {
        Invoke-WebRequest -UseBasicParsing -Uri $Url -OutFile $packagePath
    }

    $verifiedHash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash
    if ($verifiedHash -ne $Sha256) {
        throw "Checksum mismatch for $Name. Expected $Sha256, got $verifiedHash."
    }

    return $packagePath
}

function Expand-CounterStrikeSharpPackage {
    param(
        [Parameter(Mandatory = $true)][string]$PackagePath,
        [switch]$TrimAnyBaseRuntimes
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
    $destinationRoot = [IO.Path]::GetFullPath($counterStrikeSharpTarget) + [IO.Path]::DirectorySeparatorChar

    try {
        foreach ($entry in $archive.Entries) {
            $entryPath = $entry.FullName.Replace('\', '/')
            $prefix = 'addons/counterstrikesharp/'
            if (-not $entryPath.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
                continue
            }

            $relativePath = $entryPath.Substring($prefix.Length)
            if ([string]::IsNullOrWhiteSpace($relativePath) -or [string]::IsNullOrWhiteSpace($entry.Name)) {
                continue
            }

            if ($TrimAnyBaseRuntimes -and
                $relativePath -match '^shared/AnyBaseLib/runtimes/([^/]+)/' -and
                $Matches[1] -notin @('linux-x64', 'win-x64')) {
                continue
            }

            $destinationPath = [IO.Path]::GetFullPath((Join-Path $counterStrikeSharpTarget $relativePath))
            if (-not $destinationPath.StartsWith($destinationRoot, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Refusing to extract outside the package root: $relativePath"
            }

            New-Item -ItemType Directory -Path (Split-Path -Parent $destinationPath) -Force | Out-Null
            $sourceStream = $entry.Open()
            try {
                $destinationStream = [IO.File]::Create($destinationPath)
                try {
                    $sourceStream.CopyTo($destinationStream)
                }
                finally {
                    $destinationStream.Dispose()
                }
            }
            finally {
                $sourceStream.Dispose()
            }
        }
    }
    finally {
        $archive.Dispose()
    }
}

$absynthiumBuildScript = Join-Path $absynthiumMenuRoot 'compile.ps1'
if (-not (Test-Path -LiteralPath $absynthiumBuildScript -PathType Leaf)) {
    throw "Absynthium_Menu build script not found at $absynthiumBuildScript. Set ABSYNTHIUM_MENU_ROOT to the repository path."
}

Write-Host "Building Absynthium_Menu from: $absynthiumMenuRoot"
& $absynthiumBuildScript -Configuration Release

$absynthiumBuildApi = Join-Path $absynthiumMenuCompiledRoot 'shared/Absynthium_MenuApi/Absynthium_MenuApi.dll'
if (-not (Test-Path -LiteralPath $absynthiumBuildApi -PathType Leaf)) {
    throw "Absynthium_Menu API build output not found at $absynthiumBuildApi"
}

$buildApiTarget = Join-Path $root 'lib/Absynthium_MenuApi.dll'
New-Item -ItemType Directory -Path (Split-Path -Parent $buildApiTarget) -Force | Out-Null
Copy-Item -LiteralPath $absynthiumBuildApi -Destination $buildApiTarget -Force

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

if (Test-Path $absynthiumMenuCompiledRoot) {
    foreach ($relativePath in @(
        'plugins/Absynthium_MenuCore',
        'shared/Absynthium_MenuApi'
    )) {
        $sourcePath = Join-Path $absynthiumMenuCompiledRoot $relativePath
        if (-not (Test-Path $sourcePath)) {
            throw "Required Absynthium_Menu component not found: $sourcePath"
        }

        $targetPath = Join-Path $counterStrikeSharpTarget $relativePath
        New-Item -ItemType Directory -Path (Split-Path -Parent $targetPath) -Force | Out-Null
        Copy-Item -Path $sourcePath -Destination (Split-Path -Parent $targetPath) -Recurse -Force
        Write-Host " - Absynthium_Menu component copied to: $targetPath"
    }
} else {
    throw "Absynthium_Menu compiled output not found at $absynthiumMenuCompiledRoot."
}

$absynthiumConfigTarget = Join-Path $counterStrikeSharpTarget 'configs/plugins/Absynthium_MenuCore'
New-Item -ItemType Directory -Path $absynthiumConfigTarget -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'Resources/Absynthium_MenuCore.example.json') `
    -Destination (Join-Path $absynthiumConfigTarget 'Absynthium_MenuCore.example.json') -Force

$playerSettingsPackage = Get-VerifiedPackage `
    -Name 'PlayerSettings-0.9.4.zip' `
    -Url 'https://github.com/NickFox007/PlayerSettingsCS2/releases/download/0.9.4/PlayerSettings.zip' `
    -Sha256 '6D6645F728DBE07CA37264AB55F228AB4DCB3740CBA02C52FBB27EF15FAED0AC'
Expand-CounterStrikeSharpPackage -PackagePath $playerSettingsPackage

$anyBasePackage = Get-VerifiedPackage `
    -Name 'AnyBaseLib-0.9.4.zip' `
    -Url 'https://github.com/NickFox007/AnyBaseLibCS2/releases/download/0.9.4/AnyBaseLib.zip' `
    -Sha256 'AB3190F43D7AFC95D609BBB755279552FBA0283CE0A2681C371601A4BD7867FF'
Expand-CounterStrikeSharpPackage -PackagePath $anyBasePackage -TrimAnyBaseRuntimes

# Zip the staged plugin + shared folder for convenience
$zipPath = Join-Path $compiledRoot "$pluginName.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
Compress-Archive -Path (Join-Path $compiledRoot 'counterstrikesharp/*') -DestinationPath $zipPath

Write-Host "[OK] Build finished."
Write-Host " - Folder: $pluginTarget"
Write-Host " - Absynthium_Menu source: $absynthiumMenuCompiledRoot"
Write-Host " - Bundled dependencies: PlayerSettings 0.9.4, AnyBaseLib 0.9.4"
Write-Host " - Zip:    $zipPath"
