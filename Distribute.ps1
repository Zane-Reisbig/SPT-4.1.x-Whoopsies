$DistributionPath = "E:\Local Media\Games\SinglePlayerTarkov\Latest\SPT_Runtime\user\mods"
$Configuration = 'Release'

$ErrorActionPreference = 'Stop'

$source = Join-Path $PSScriptRoot "bin\$Configuration\Whoopises"
if (-not (Test-Path $source)) {
    throw "Build output not found: $source (run Build.ps1 first)"
}

if (-not (Test-Path $DistributionPath)) {
    New-Item -ItemType Directory -Path $DistributionPath | Out-Null
}

$target = Join-Path $DistributionPath 'Whoopises'
if (Test-Path $target) {
    Remove-Item $target -Recurse -Force
}

Move-Item $source $target
