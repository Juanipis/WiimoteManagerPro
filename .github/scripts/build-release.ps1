param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = "Stop"

if (Test-Path ".\\publish") {
    Remove-Item ".\\publish" -Recurse -Force
}

if (Test-Path ".\\WiimoteManager-win64.zip") {
    Remove-Item ".\\WiimoteManager-win64.zip" -Force
}

dotnet publish WiiMoteUtlity\WiimoteManager\WiimoteManager.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -o .\publish `
  /p:Version=$Version

Compress-Archive -Path .\publish\* -DestinationPath .\WiimoteManager-win64.zip
