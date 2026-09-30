$ErrorActionPreference = 'Stop'
$installRoot = $PSScriptRoot
$installStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$installLog = Join-Path $installRoot ('android-elevated-' + $installStamp + '.log')
$installErrorLog = Join-Path $installRoot ('android-elevated-' + $installStamp + '.err')
try {
    $installProcess = Start-Process -FilePath 'C:\Program Files\Unity Hub\Unity Hub.exe' -ArgumentList @('--','--headless','install-modules','--version','6000.0.58f2','--module','android','--childModules') -WindowStyle Hidden -PassThru -RedirectStandardOutput $installLog -RedirectStandardError $installErrorLog
    @{ id=$installProcess.Id; path='C:\Program Files\Unity Hub\Unity Hub.exe'; startedAt=(Get-Date).ToString('o'); log=$installLog; errorLog=$installErrorLog; elevated=$true } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $installRoot 'android-elevated-process.json') -Encoding utf8
    $installProcess.WaitForExit()
    @{ finishedAt=(Get-Date).ToString('o'); exitCode=$installProcess.ExitCode; log=$installLog; errorLog=$installErrorLog } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $installRoot 'android-elevated-result.json') -Encoding utf8
} catch {
    @{ finishedAt=(Get-Date).ToString('o'); error=$_.Exception.Message } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $installRoot 'android-elevated-result.json') -Encoding utf8
    exit 1
}
