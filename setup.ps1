# UCHWiiRemoteMod Installer/Setup Script
# Creates a desktop shortcut for Wiimote Manager Pro

$scriptPath = $MyInvocation.MyCommand.Path
$directory = Split-Path $scriptPath
$executablePath = Join-Path $directory "WiimoteManager.exe"

if (-not (Test-Path $executablePath)) {
    # If not in same folder as exe, try to find it in default build output or publish folder
    $possiblePaths = @(
        Join-Path $directory "WiiMoteUtlity\WiimoteManager\bin\Release\net8.0-windows10.0.19041.0\win-x64\WiimoteManager.exe",
        Join-Path $directory "WiiMoteUtlity\WiimoteManager\bin\Debug\net8.0-windows10.0.19041.0\WiimoteManager.exe"
    )

    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            $executablePath = $path
            break
        }
    }
}

if (-not (Test-Path $executablePath)) {
    Write-Host "Error: Could not find WiimoteManager.exe" -ForegroundColor Red
    Write-Host "Please build the project first or place this script in the application folder."
    exit 1
}

$WshShell = New-Object -comObject WScript.Shell
$DesktopPath = $WshShell.SpecialFolders.Item("Desktop")
$ShortcutPath = Join-Path $DesktopPath "Wiimote Manager Pro.lnk"
$Shortcut = $WshShell.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath = $executablePath
$Shortcut.Description = "Professional Wiimote Manager for Windows"
$Shortcut.IconLocation = $executablePath
$Shortcut.WorkingDirectory = Split-Path $executablePath
$Shortcut.Save()

Write-Host "Successfully created shortcut on Desktop!" -ForegroundColor Green
Write-Host "You can now launch Wiimote Manager Pro from your desktop."
Start-Sleep -Seconds 3
