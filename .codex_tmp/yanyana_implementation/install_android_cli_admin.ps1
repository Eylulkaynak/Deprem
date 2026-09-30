$ErrorActionPreference = 'Stop'
$installStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$installLog = Join-Path $PSScriptRoot ('android-cli-admin-' + $installStamp + '.log')
$installErr = Join-Path $PSScriptRoot ('android-cli-admin-' + $installStamp + '.err')
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) { throw 'This installer needs the normal Windows administrator approval.' }
$installExe = Join-Path $env:LOCALAPPDATA 'Microsoft/WindowsApps/unity.exe'
$installProcess = Start-Process -FilePath $installExe -ArgumentList @('install-modules','-e','6000.0.58f2','-m','android','--child-modules','--accept-eula','--non-interactive','--format','ndjson','--no-banner','--no-elevate') -PassThru -WindowStyle Hidden -RedirectStandardOutput $installLog -RedirectStandardError $installErr
@{ id=$installProcess.Id; administrator=$isAdmin; startedAt=(Get-Date).ToString('o'); path=$installExe; log=$installLog; errorLog=$installErr } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'android-cli-admin-process.json') -Encoding utf8
$installProcess.WaitForExit()
@{ finishedAt=(Get-Date).ToString('o'); exitCode=$installProcess.ExitCode; log=$installLog; errorLog=$installErr } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'android-cli-admin-result.json') -Encoding utf8
