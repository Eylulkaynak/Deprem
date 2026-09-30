$ErrorActionPreference = 'Stop'
$sourceRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$setupRecord = Join-Path $sourceRoot '.codex_tmp/yanyana_implementation/android-stage-current.json'
$stageInfo = Get-Content -LiteralPath $setupRecord -Raw | ConvertFrom-Json
$expectedTarget = 'C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Data\PlaybackEngines\AndroidPlayer'
$targetRoot = (Resolve-Path -LiteralPath $stageInfo.target).Path
$stageRoot = (Resolve-Path -LiteralPath $stageInfo.stage).Path
$expectedStagePrefix = (Join-Path $env:LOCALAPPDATA 'YanYanaAndroidSetup') + [IO.Path]::DirectorySeparatorChar
if ($stageInfo.purpose -ne 'YanYana official Android toolchain stage' -or $targetRoot -ne $expectedTarget -or -not $stageRoot.StartsWith($expectedStagePrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected toolchain source or destination.' }
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) { throw 'Windows administrator approval is required to install into this Editor.' }
$setupLog = Join-Path $sourceRoot 'ClientExports/YanYana/Reports/android-toolchain-install.txt'
$setupResult = Join-Path $sourceRoot '.codex_tmp/yanyana_implementation/android-toolchain-install-result.json'
try {
    ('Started=' + (Get-Date).ToString('o')) | Set-Content -LiteralPath $setupLog -Encoding utf8
    foreach ($folder in @('OpenJDK', 'NDK', 'SDK')) {
        $copySource = Join-Path $stageRoot $folder
        $copyTarget = [IO.Path]::GetFullPath((Join-Path $targetRoot $folder))
        if (-not $copyTarget.StartsWith($expectedTarget + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Copy target escaped AndroidPlayer.' }
        & 'C:\Windows\System32\robocopy.exe' $copySource $copyTarget /E /COPY:DAT /DCOPY:DAT /R:2 /W:1 /NFL /NDL /NJH /NJS /NP | Add-Content -LiteralPath $setupLog
        if ($LASTEXITCODE -ge 8) { throw ('Copy failed for ' + $folder + ': ' + $LASTEXITCODE) }
        ('Installed=' + $folder) | Add-Content -LiteralPath $setupLog
    }
    # Module EULAs were already accepted in the authorized CLI installation.
    $env:JAVA_HOME = Join-Path $targetRoot 'OpenJDK'
    $sdkManager = Join-Path $targetRoot 'SDK/cmdline-tools/16.0/bin/sdkmanager.bat'
    $sdkRoot = Join-Path $targetRoot 'SDK'
    1..50 | ForEach-Object { 'y' } | & $sdkManager ('--sdk_root=' + $sdkRoot) --licenses 2>&1 | Add-Content -LiteralPath $setupLog
    if ($LASTEXITCODE -ne 0) { throw ('SDK license registration failed: ' + $LASTEXITCODE) }
    @{ success=$true; finishedAt=(Get-Date).ToString('o'); target=$targetRoot; stage=$stageRoot; log=$setupLog } | ConvertTo-Json | Set-Content -LiteralPath $setupResult -Encoding utf8
} catch {
    @{ success=$false; finishedAt=(Get-Date).ToString('o'); error=$_.Exception.Message; log=$setupLog } | ConvertTo-Json | Set-Content -LiteralPath $setupResult -Encoding utf8
    exit 1
}
