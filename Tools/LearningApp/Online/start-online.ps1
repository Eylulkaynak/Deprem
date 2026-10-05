param(
    [ValidatePattern('^[A-Za-z0-9.:-]+$')][string]$BindAddress = '127.0.0.1',
    [ValidateRange(1024,65535)][int]$Port = 8765,
    [string]$DatabasePath
)
$ErrorActionPreference = 'Stop'
$serviceDirectory = [System.IO.Path]::GetFullPath($PSScriptRoot)
$serverPath = Join-Path $serviceDirectory 'server.py'
$dataDirectory = Join-Path $serviceDirectory 'data'
New-Item -ItemType Directory -Force -Path $dataDirectory | Out-Null
if ([string]::IsNullOrWhiteSpace($DatabasePath)) { $DatabasePath = Join-Path $dataDirectory 'online.sqlite3' }
$DatabasePath = [System.IO.Path]::GetFullPath($DatabasePath)
$checkAddress = if ($BindAddress -eq '0.0.0.0') { '127.0.0.1' } else { $BindAddress }
try {
    $existing = Invoke-RestMethod -Uri "http://${checkAddress}:$Port/health" -TimeoutSec 2
    if ($existing.service -eq 'deprem-online') { Write-Output "Deprem Online zaten çalışıyor: ${checkAddress}:$Port"; exit 0 }
} catch { }
$pythonPath = (Get-Command python -ErrorAction Stop).Source
$arguments = @('-u',('"' + $serverPath + '"'),'--host',$BindAddress,'--port',$Port,'--db',('"' + $DatabasePath + '"'))
$process = Start-Process -FilePath $pythonPath -ArgumentList $arguments -WindowStyle Hidden -PassThru -WorkingDirectory $serviceDirectory -RedirectStandardOutput (Join-Path $dataDirectory "server-$Port.log") -RedirectStandardError (Join-Path $dataDirectory "server-$Port.error.log")
Set-Content -LiteralPath (Join-Path $dataDirectory "server-$Port.pid") -Value $process.Id
for ($attempt = 0; $attempt -lt 20; $attempt++) {
    Start-Sleep -Milliseconds 250
    try {
        $health = Invoke-RestMethod -Uri "http://${checkAddress}:$Port/health" -TimeoutSec 1
        if ($health.service -eq 'deprem-online') { Write-Output "Deprem Online hazır: ${checkAddress}:$Port (PID $($process.Id))"; exit 0 }
    } catch { }
    if ($process.HasExited) { throw "Sunucu açılamadı. data/server-$Port.error.log dosyasını kontrol et." }
}
throw "Sunucu yanıt vermedi. data/server-$Port.error.log dosyasını kontrol et."
