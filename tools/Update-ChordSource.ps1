<#
.SYNOPSIS
    Downloads tombatossals/chords-db guitar.json pinned to a specific commit SHA
    and vendors it into FRELODYAPIs/wwwroot/seed-chords/_source/.

.DESCRIPTION
    Single source of truth for our standard chord catalog. The file is committed
    to the repo so seeding never depends on GitHub at runtime. Update the pinned
    SHA below when you want to refresh the catalog, then re-run this script.

.PARAMETER Sha
    Commit SHA to pin to. Defaults to the value baked into this script.

.PARAMETER ExpectedHash
    Optional SHA256 of the downloaded file for verification. If supplied, the
    script aborts on mismatch. If omitted, the actual hash is printed for you
    to capture and bake in on the next refresh.
#>

[CmdletBinding()]
param(
    [string]$Sha = 'df06fa7b425cf5fd29485ff6591236b3557e3fac',
    [string]$ExpectedHash = 'cfe439962b2f444d2c341b1f0261403b4c3a3416e321147286fc608922699974'
)

$ErrorActionPreference = 'Stop'

$repoRoot   = Split-Path -Parent $PSScriptRoot
$targetDir  = Join-Path $repoRoot 'FRELODYAPIs\wwwroot\seed-chords\_source'
$targetFile = Join-Path $targetDir 'chords-db.guitar.json'
$url        = "https://raw.githubusercontent.com/tombatossals/chords-db/$Sha/lib/guitar.json"

if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
}

Write-Host "Fetching $url"
Invoke-WebRequest -Uri $url -OutFile $targetFile -UseBasicParsing

$actualHash = (Get-FileHash -Path $targetFile -Algorithm SHA256).Hash.ToLower()
$sizeKB     = [Math]::Round((Get-Item $targetFile).Length / 1024, 1)

Write-Host ""
Write-Host "Downloaded: $targetFile"
Write-Host "Size:       $sizeKB KB"
Write-Host "Pinned SHA: $Sha"
Write-Host "SHA256:     $actualHash"

if ($ExpectedHash) {
    if ($actualHash -ne $ExpectedHash.ToLower()) {
        Remove-Item $targetFile -Force
        throw "SHA256 mismatch. Expected $ExpectedHash, got $actualHash. File removed."
    }
    Write-Host "Hash verified."
}
