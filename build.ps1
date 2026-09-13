param(
    [string]$Configuration = 'Release',
    [string]$OutputDirectory = 'dist'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$output = [IO.Path]::GetFullPath((Join-Path $root $OutputDirectory))
New-Item -ItemType Directory -Path $output -Force | Out-Null

dotnet publish (Join-Path $root 'ChatGPTAdminLauncher.csproj') `
    -c $Configuration `
    -o $output
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$built = Join-Path $output 'ChatGPTAdminLauncher.exe'
$release = Join-Path $output 'ChatGPT-Admin-Launcher-win-x64.exe'
Copy-Item -LiteralPath $built -Destination $release -Force
$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $release).Hash.ToLowerInvariant()
Set-Content -LiteralPath (Join-Path $output 'SHA256SUMS.txt') `
    -Value "$hash  ChatGPT-Admin-Launcher-win-x64.exe" `
    -Encoding ascii

Write-Host "Built: $release"
Write-Host "SHA-256: $hash"
