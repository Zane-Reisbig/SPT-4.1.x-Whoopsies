$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$bin = Join-Path $PSScriptRoot 'bin'
if (Test-Path $bin) {
    Remove-Item $bin -Recurse -Force
}

foreach ($configuration in 'Debug', 'Release') {
    dotnet build -c $configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
