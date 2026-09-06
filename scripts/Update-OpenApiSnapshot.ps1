<#
.SYNOPSIS
  Re-exports the OpenAPI snapshot (src/TextBox.Sdk/openapi.json) from a running server.
.DESCRIPTION
  Starts the host, waits for /openapi/v1.json, saves it UTF-8 without BOM,
  then stops the server. Run with a key configured (the default
  appsettings.json has one) so the snapshot keeps the Bearer scheme.
  Follow with ./scripts/Regen-SdkClient.ps1 after API changes.
.EXAMPLE
  ./scripts/Update-OpenApiSnapshot.ps1
.EXAMPLE
  ./scripts/Update-OpenApiSnapshot.ps1 -Port 5000 -NoBuild
#>
param(
    [int]$Port = 5031,
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$out = Join-Path $root 'src\TextBox.Sdk\openapi.json'
$url = "http://localhost:$Port/openapi/v1.json"

$runArgs = @('run', '--project', 'src/TextBox/TextBox.csproj', '--urls', "http://localhost:$Port")
if ($NoBuild) { $runArgs += '--no-build' }

$job = Start-Job -ScriptBlock {
    param($workingDir, $dotnetArgs)
    Set-Location -LiteralPath $workingDir
    dotnet @dotnetArgs
} -ArgumentList $root, $runArgs

try {
    $deadline = (Get-Date).AddSeconds(180)
    $response = $null
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 3
        try {
            $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 5
            break
        } catch {
            $response = $null
        }
    }
    if ($null -eq $response) {
        Receive-Job $job | Select-Object -Last 15
        throw "Server did not serve $url in time."
    }

    $json = $response.Content
    $null = $json | ConvertFrom-Json  # validates the payload
    [IO.File]::WriteAllText($out, $json, (New-Object Text.UTF8Encoding $false))

    if ($json -notmatch '"ApiKey"') {
        Write-Warning 'Snapshot has no ApiKey scheme — export with a key configured (ApiKey:Key) to document auth.'
    }
    "Snapshot written: $out ($($json.Length) chars)"
} finally {
    Stop-Job $job
    Remove-Job $job
}
