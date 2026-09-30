$ErrorActionPreference = 'Stop'
$reviewRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$playerPath = (Resolve-Path -LiteralPath (Join-Path $reviewRoot 'ClientExports/YanYana/Windows/YanYana.exe')).Path
$expectedPlayer = Join-Path $reviewRoot 'ClientExports/YanYana/Windows/YanYana.exe'
if (-not $playerPath.Equals($expectedPlayer, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected player path.' }
$logPath = Join-Path $reviewRoot 'ClientExports/YanYana/Reports/windows-startup.log'
if (Test-Path -LiteralPath $logPath) {
    $priorLog = Join-Path $reviewRoot ('ClientExports/YanYana/Reports/windows-startup-before-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '.log')
    Move-Item -LiteralPath $logPath -Destination $priorLog
}
$playerArguments = @('-screen-width', '540', '-screen-height', '960', '-screen-fullscreen', '0', '-logFile', ('"' + $logPath + '"'))
$ownedPlayer = Start-Process -FilePath $playerPath -WorkingDirectory (Split-Path -Parent $playerPath) -ArgumentList $playerArguments -WindowStyle Hidden -PassThru
$ownedStart = $ownedPlayer.StartTime.ToUniversalTime()
$ready = $false
try {
    $deadline = (Get-Date).AddSeconds(45)
    while ((Get-Date) -lt $deadline) {
        $ownedPlayer.Refresh()
        if ($ownedPlayer.HasExited) { throw 'Player exited before startup completed.' }
        if (Test-Path -LiteralPath $logPath) {
            $startupText = Get-Content -LiteralPath $logPath -Raw
            if ($startupText -match 'Exception:|NullReferenceException|MissingMethodException|Failed to create agent|Shader error') { throw 'Startup error; inspect windows-startup.log.' }
            if ($startupText.Contains('YAN YANA navigation ready: True')) { $ready = $true; break }
        }
        Start-Sleep -Milliseconds 500
    }
    if (-not $ready) { throw 'No navigation startup confirmation within 45 seconds.' }
    Start-Sleep -Seconds 2
    $startupText = Get-Content -LiteralPath $logPath -Raw
    if (-not $startupText.Contains('YAN YANA navigation ready: True')) { throw 'Fresh startup confirmation disappeared.' }
    if ($startupText -match 'Exception:|NullReferenceException|MissingMethodException|Failed to create agent|Shader error') { throw 'Startup error after navigation became ready.' }
    @{ checkedAt = (Get-Date).ToString('o'); passed = $true; processId = $ownedPlayer.Id; path = $playerPath; startedUtc = $ownedStart.ToString('o'); scope = 'Fresh standalone graphics and navigation startup; full interaction routes are Editor checks.' } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $reviewRoot 'ClientExports/YanYana/Reports/windows-startup-smoke.json') -Encoding utf8
    'PASS standalone graphics/navigation startup without exceptions.'
}
finally {
    $running = Get-Process -Id $ownedPlayer.Id -ErrorAction SilentlyContinue
    if ($running) {
        if (-not $running.Path.Equals($playerPath, [StringComparison]::OrdinalIgnoreCase) -or $running.StartTime.ToUniversalTime() -ne $ownedStart) { throw 'Process identity changed; refusing to stop it.' }
        Stop-Process -Id $running.Id
    }
}
