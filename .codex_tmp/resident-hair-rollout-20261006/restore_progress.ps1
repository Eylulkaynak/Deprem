$taskBackup = $PSScriptRoot
$saved = Import-Clixml -LiteralPath (Join-Path $taskBackup 'playerprefs.xml')
$scope = @($saved.PSObject.Properties | Where-Object Name -Like 'Deprem.YanYana.v1*')
$names = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($property in $scope) { [void]$names.Add($property.Name) }
$prefs = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Unity\UnityEditor\DepremEgitim\Deprem', $true)
try {
    foreach ($name in $prefs.GetValueNames()) {
        if ($name -like 'Deprem.YanYana.v1*' -and -not $names.Contains($name)) { $prefs.DeleteValue($name) }
    }
    foreach ($property in $scope) {
        $value = $property.Value
        if ($value -is [string]) { $prefs.SetValue($property.Name, [string]$value, [Microsoft.Win32.RegistryValueKind]::String) }
        elseif ($value -is [byte[]]) { $prefs.SetValue($property.Name, [byte[]]$value, [Microsoft.Win32.RegistryValueKind]::Binary) }
        elseif ($value -is [uint32]) {
            $signed = [BitConverter]::ToInt32([BitConverter]::GetBytes([uint32]$value), 0)
            $prefs.SetValue($property.Name, [int]$signed, [Microsoft.Win32.RegistryValueKind]::DWord)
        }
        elseif ($value -is [int]) { $prefs.SetValue($property.Name, [int]$value, [Microsoft.Win32.RegistryValueKind]::DWord) }
        else { throw "Unexpected value type for $($property.Name): $($value.GetType())" }
    }
} finally { $prefs.Close() }
$profile = 'C:\Users\Gokturk\AppData\LocalLow\DepremEgitim\Deprem\Deprem.YanYana.v1.activities.json'
Copy-Item -LiteralPath (Join-Path $taskBackup 'activities.json') -Destination $profile
$before = (Get-FileHash -LiteralPath (Join-Path $taskBackup 'activities.json')).Hash
$after = (Get-FileHash -LiteralPath $profile).Hash
if ($before -ne $after) { throw 'Activity profile did not restore exactly.' }
"Restored $($scope.Count) scoped preferences and exact activity profile."
