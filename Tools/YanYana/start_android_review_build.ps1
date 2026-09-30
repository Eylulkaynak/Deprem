$ErrorActionPreference = 'Stop'
$sourceRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$copyRecord = Join-Path $sourceRoot '.codex_tmp/yanyana_implementation/android-project-current.json'
$copyInfo = Get-Content -LiteralPath $copyRecord -Raw | ConvertFrom-Json
$copyRoot = (Resolve-Path -LiteralPath $copyInfo.clone).Path
$scratchPrefix = (Join-Path $sourceRoot '.codex_tmp') + [IO.Path]::DirectorySeparatorChar
if (-not $copyRoot.StartsWith($scratchPrefix, [StringComparison]::OrdinalIgnoreCase) -or $copyInfo.source -ne $sourceRoot) {
    throw 'Refusing to run an Android build outside the prepared project copy.'
}
$copyMarker = Get-Content -LiteralPath (Join-Path $copyRoot 'yanyana-android-copy.json') -Raw | ConvertFrom-Json
if ($copyMarker.clone -ne $copyRoot -or $copyMarker.purpose -ne 'YanYana isolated Android review build') { throw 'Invalid Android copy marker.' }
$existingCopyEditor = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.IndexOf($copyRoot, [StringComparison]::OrdinalIgnoreCase) -ge 0 }
if ($existingCopyEditor) { throw 'This project copy already has an Editor process; inspect it instead of launching twice.' }
$androidRoot = 'C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Data\PlaybackEngines\AndroidPlayer'
foreach ($required in @('UnityEditor.Android.Extensions.dll', 'OpenJDK/bin/java.exe', 'NDK/toolchains/llvm/prebuilt/windows-x86_64/bin/clang.exe', 'SDK/platform-tools/adb.exe', 'SDK/build-tools/34.0.0/aapt2.exe', 'SDK/platforms/android-35/android.jar')) {
    if (-not (Test-Path -LiteralPath (Join-Path $androidRoot $required))) { throw ('Android component is missing: ' + $required) }
}
$scenePath = Join-Path $sourceRoot $copyInfo.scene
if ((Get-FileHash -LiteralPath $scenePath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $copyInfo.sceneSha256) { throw 'The source scene changed after the copy. Prepare a fresh copy.' }
$template = Join-Path $PSScriptRoot 'YanYanaAndroidReviewBuild.cs.template'
$copyHelper = Join-Path $copyRoot 'Assets/YanYana/Editor/YanYanaAndroidReviewBuild.cs'
Copy-Item -LiteralPath $template -Destination $copyHelper
$copyInfo.helperSha256 = (Get-FileHash -LiteralPath $copyHelper -Algorithm SHA256).Hash.ToLowerInvariant()
$copyInfo | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $copyRecord -Encoding utf8
$copyInfo | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $copyRoot 'yanyana-android-copy.json') -Encoding utf8
$buildStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$buildLog = Join-Path $sourceRoot ('ClientExports/YanYana/Reports/android-editor-' + $buildStamp + '.log')
$editorExe = 'C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Unity.exe'
$buildArguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $copyRoot + '"'), '-buildTarget', 'Android', '-executeMethod', 'YanYana.Editor.YanYanaAndroidReviewBuild.Build', '-quit', '-logFile', ('"' + $buildLog + '"'))
$buildProcess = Start-Process -FilePath $editorExe -WorkingDirectory $copyRoot -ArgumentList $buildArguments -WindowStyle Hidden -PassThru
@{ id=$buildProcess.Id; path=$editorExe; project=$copyRoot; log=$buildLog; startedAt=(Get-Date).ToString('o'); sceneSha256=$copyInfo.sceneSha256 } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $sourceRoot '.codex_tmp/yanyana_implementation/android-build-process.json') -Encoding utf8
Get-Content -LiteralPath (Join-Path $sourceRoot '.codex_tmp/yanyana_implementation/android-build-process.json') -Raw
