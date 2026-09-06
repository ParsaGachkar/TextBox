<#
.SYNOPSIS
  Seeds fake SMS data so the dashboard (/ phone panel) has something to show.
.DESCRIPTION
  POSTs a canned set of conversations to /api/messages. Use -ClearFirst to
  wipe the store before seeding, and -ApiKey when the server requires one.
.EXAMPLE
  ./scripts/Seed-FakeData.ps1
.EXAMPLE
  ./scripts/Seed-FakeData.ps1 -BaseAddress http://localhost:8080 -ApiKey secret-1 -ClearFirst
#>
param(
    [string]$BaseAddress = 'http://localhost:5031',
    [string]$ApiKey = '',
    [switch]$ClearFirst
)

$ErrorActionPreference = 'Stop'

$headers = @{ }
if (-not [string]::IsNullOrWhiteSpace($ApiKey)) {
    $headers['Authorization'] = "Bearer $($ApiKey.Trim())"
}

function Invoke-Api([string]$Method, [string]$Path, [string]$Body) {
    $params = @{
        Uri = "$BaseAddress$Path"
        Method = $Method
        Headers = $headers
        ContentType = 'application/json'
        UseBasicParsing = $true
        TimeoutSec = 10
    }
    if ($null -ne $Body) { $params['Body'] = [Text.Encoding]::UTF8.GetBytes($Body) }
    try {
        return Invoke-WebRequest @params
    } catch {
        throw "API call $Method $Path failed: $($_.Exception.Message)"
    }
}

if ($ClearFirst) {
    $cleared = Invoke-Api 'DELETE' '/api/messages' $null | ConvertFrom-Json
    "Cleared $($cleared.cleared) message(s)."
}

$seeds = @(
    @{ To = '+123'; From = 'Alice'; Body = 'hey, is the mock up?' },
    @{ To = '+123'; From = 'Alice'; Body = 'looks good on my side' },
    @{ To = '+123'; From = ''; Body = 'ack - running local' },
    @{ To = '+15551234567'; From = 'Bob'; Body = 'your code is 481516' },
    @{ To = '+15551234567'; From = ''; Body = 'got it, thanks!' },
    @{ To = '+447700900123'; From = 'Carol'; Body = 'meeting moved to 3pm' },
    @{ To = '+447700900123'; From = 'Carol'; Body = 'bring the demo phone' },
    @{ To = '+1000'; From = ''; Body = 'smoke test, ignore me' }
)

$sent = 0
foreach ($s in $seeds) {
    $payload = @{ to = $s.To; from = $s.From; body = $s.Body } | ConvertTo-Json -Compress
    $response = Invoke-Api 'POST' '/api/messages' $payload
    if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) { $sent++ }
}

"Seeded $sent/$($seeds.Count) message(s) at $BaseAddress."
