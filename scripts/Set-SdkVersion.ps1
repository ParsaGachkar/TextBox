<#
.SYNOPSIS
  Bumps the TextBox.Sdk <Version> in src/TextBox.Sdk/TextBox.Sdk.csproj.
.EXAMPLE
  ./scripts/Set-SdkVersion.ps1 -Version 0.2.0
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

if ($Version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z\.-]+)?$') {
    throw "Version '$Version' is not SemVer (expected e.g. 0.2.0 or 1.0.0-beta.1)."
}

$csproj = Join-Path $PSScriptRoot '..\src\TextBox.Sdk\TextBox.Sdk.csproj'
$csproj = (Resolve-Path $csproj).Path

[xml]$xml = Get-Content -LiteralPath $csproj
$node = $xml.SelectSingleNode('/Project/PropertyGroup/Version')
if ($null -eq $node) {
    throw "No <Version> element found in $csproj."
}

$old = $node.InnerText
$node.InnerText = $Version
$xml.Save($csproj)

"TextBox.Sdk version: $old -> $Version"
