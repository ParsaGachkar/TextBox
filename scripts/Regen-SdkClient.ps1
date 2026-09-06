<#
.SYNOPSIS
  Regenerates the NSwag client (Generated/TextBoxClient.g.cs) from openapi.json.
.DESCRIPTION
  Paths in nswag.json resolve from the working directory, so this runs
  nswag from src/TextBox.Sdk. Requires the NSwag CLI:
  dotnet tool install -g NSwag.ConsoleCore --version 14.7.1
.EXAMPLE
  ./scripts/Regen-SdkClient.ps1
#>

$ErrorActionPreference = 'Stop'

$dir = (Resolve-Path (Join-Path $PSScriptRoot '..\src\TextBox.Sdk')).Path

try {
    if ($null -eq (Get-Command nswag -ErrorAction SilentlyContinue)) {
        throw 'nswag CLI not found. Install it: dotnet tool install -g NSwag.ConsoleCore --version 14.7.1'
    }
    Push-Location -LiteralPath $dir
    nswag run nswag.json
} finally {
    Pop-Location
}
