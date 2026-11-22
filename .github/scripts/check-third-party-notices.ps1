# Checks that THIRD-PARTY-NOTICES.txt has a "Package:" line for every package the app ships, and no others.
# Run it from the repo root after dotnet build.

$ErrorActionPreference = 'Stop'

# The build lists the packages the app loads in this file, without build-only packages such as analyzers.
$shippedPackagesFile = 'src/ClockShot.App/bin/Debug/net10.0-windows/ClockShot.deps.json'
$noticesFile = 'src/ClockShot.App/THIRD-PARTY-NOTICES.txt'

$libraries = (Get-Content $shippedPackagesFile -Raw | ConvertFrom-Json).libraries.PSObject.Properties
$shipped = $libraries | Where-Object { $_.Value.type -eq 'package' } | ForEach-Object { $_.Name.Split('/')[0] }
$named = Select-String $noticesFile -Pattern '^Package: (.+)$' | ForEach-Object { $_.Matches[0].Groups[1].Value }

$missing = $shipped | Where-Object { $_ -notin $named }
$noLongerShipped = $named | Where-Object { $_ -notin $shipped }

if ($missing) {
    Write-Host "Add these packages and their licences to ${noticesFile}: $missing"
}
if ($noLongerShipped) {
    Write-Host "Remove these packages from $noticesFile, because they no longer ship: $noLongerShipped"
}
if ($missing -or $noLongerShipped) {
    exit 1
}
