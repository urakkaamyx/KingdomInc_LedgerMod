param([switch]$StageForNextLaunch)
$ErrorActionPreference = 'Stop'
$gameRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
if (-not (Test-Path -LiteralPath (Join-Path $gameRoot 'BepInEx\plugins\Foundation\Foundation.dll'))) { throw 'Install Foundation 0.5.0 or later before Ledger.' }
dotnet build (Join-Path $PSScriptRoot 'LedgerMod.csproj') -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Ledger build failed.' }
$destination = Join-Path $gameRoot 'BepInEx\plugins\LedgerMod'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$dllPath = Join-Path $destination 'LedgerMod.dll'
if (Test-Path -LiteralPath $dllPath) {
    $running = Get-Process KingdomInc -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq (Join-Path $gameRoot 'KingdomInc.exe') }
    if ($running -and -not $StageForNextLaunch) { throw 'Close this game installation, or use -StageForNextLaunch to preserve the mapped DLL and stage a fresh next-launch file.' }
    $backup = Join-Path $PSScriptRoot ('_Backup\' + (Get-Date -Format 'yyyyMMdd-HHmmss-ffff'))
    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    Copy-Item -LiteralPath $dllPath -Destination (Join-Path $backup 'LedgerMod.dll')
    if ($running) {
        # Preserve the loaded file intact outside plugin discovery. The running
        # process keeps its original mapping; a fresh file is used on next launch.
        $resolvedDll = (Resolve-Path -LiteralPath $dllPath).Path
        $resolvedBackup = (Resolve-Path -LiteralPath $backup).Path
        if (-not $resolvedDll.StartsWith($gameRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
            -not $resolvedBackup.StartsWith($gameRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Install paths escaped game root.' }
        $loadedDll = Join-Path $resolvedBackup 'LedgerMod.loaded.dll'
        Move-Item -LiteralPath $resolvedDll -Destination $loadedDll -ErrorAction Stop
        try {
            Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'bin\Release\netstandard2.1\LedgerMod.dll') -Destination $dllPath -ErrorAction Stop
        } catch {
            if (-not (Test-Path -LiteralPath $dllPath)) { Move-Item -LiteralPath $loadedDll -Destination $dllPath -ErrorAction Stop }
            throw
        }
        Write-Output "Staged for next launch: $dllPath"
        Write-Output "Running game retains its previous mapped DLL: $loadedDll"
        exit 0
    }
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'bin\Release\netstandard2.1\LedgerMod.dll') -Destination $dllPath -Force
Write-Output "Installed for next launch: $dllPath"
