param(
    [string]$GameRoot = (Join-Path $PSScriptRoot '..\..\..')
)

$ErrorActionPreference = 'Stop'
$gamePath = (Resolve-Path -LiteralPath $GameRoot).Path.TrimEnd('\', '/')
$project = Join-Path $PSScriptRoot 'LedgerMod.csproj'

& dotnet build $project -c Release "-p:GameRoot=$($gamePath.Replace('\', '/') + '/')"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Built: $(Join-Path $PSScriptRoot 'bin\Release\netstandard2.1\LedgerMod.dll')"
