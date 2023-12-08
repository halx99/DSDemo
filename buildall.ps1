# custom installed editors
$editors_v2_conf = Join-Path $env:APPDATA 'UnityHub/editors-v2.json'
$secondaryInstallPath = Join-Path $env:APPDATA 'UnityHub/secondaryInstallPath.json'

$InstalledSoftwares = Get-ChildItem "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall"

$defaultInstallLocation = $null
foreach($inst in $InstalledSoftwares) {
    $dispName = $inst.GetValue('DisplayName')
    if ("$dispName".IndexOf('Unity Hub') -ne -1) {
        $uninstallString = $inst.GetValue('UninstallString')
        $defaultInstallLocation =  Split-Path $uninstallString.Split('.exe')[0] -Parent
        $defaultInstallLocation = Join-Path (Split-Path $defaultInstallLocation) 'Unity/Hub/Editor'
    }
}
