<#
.SYNOPSIS
  One-command verification of the container stack (enabler TE10).

.DESCRIPTION
  1. CI gate: waits for the current branch's pull request checks and stops if they are red (-SkipCi to skip).
  2. Backs up the stack's data volume, then resets it so every run starts from a known state.
  3. Rebuilds and restarts the stack and waits until it is healthy.
  4. Seeds two test administrators and exercises the operator commands (answers piped on standard input).
  5. Runs the verification container: .NET tests, Angular tests, then the runtime tests of every feature.
  Every test is logged on its own line. Results: verify-results\latest.log and verify-results\latest.json.
  The data is left in place after the run. Exit code 0 means everything passed.
#>
[CmdletBinding()]
param(
  [switch]$SkipCi,
  [int]$KeepBackups = 3
)

$ErrorActionPreference = 'Continue'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$results = Join-Path $root 'verify-results'
$volume = 'goosewebsite_web-data'

function Fail([string]$message) {
  Write-Host "VERIFY STOPPED: $message" -ForegroundColor Red
  exit 2
}

function Step([string]$message) { Write-Host "`n=== $message" -ForegroundColor Cyan }

$gh = (Get-Command gh -ErrorAction SilentlyContinue).Source
if (-not $gh -and (Test-Path 'C:\Program Files\GitHub CLI\gh.exe')) { $gh = 'C:\Program Files\GitHub CLI\gh.exe' }

# --- 1. CI gate -------------------------------------------------------------------------------------------------
Step 'CI gate (GitHub Actions is the first check)'
if ($SkipCi) {
  Write-Host 'Skipped (-SkipCi).'
} else {
  if (-not $gh) { Fail 'The GitHub CLI (gh) is needed for the CI gate. Install it or use -SkipCi.' }
  $prJson = & $gh pr view --json number,headRefOid 2>$null
  if ($LASTEXITCODE -ne 0 -or -not $prJson) { Fail 'This branch has no pull request. Push it and open one, or use -SkipCi.' }
  $pr = $prJson | ConvertFrom-Json
  $head = (git rev-parse HEAD).Trim()
  if ($pr.headRefOid -ne $head) { Fail "CI checked $($pr.headRefOid.Substring(0,7)) but HEAD is $($head.Substring(0,7)). Push first, or use -SkipCi." }
  if (git status --porcelain) { Write-Host 'Warning: uncommitted changes are verified in the container but were not checked by CI.' -ForegroundColor Yellow }
  Write-Host "Waiting for the checks of pull request #$($pr.number)..."
  & $gh pr checks $pr.number --watch
  if ($LASTEXITCODE -ne 0) { Fail "CI is not green on pull request #$($pr.number). Fix it before deploying to the container." }
}

# --- 2. Backup and reset ----------------------------------------------------------------------------------------
Step 'Backup and reset of the stack data'
New-Item -ItemType Directory -Force $results | Out-Null
Get-ChildItem $results -Filter 'latest.*' -ErrorAction SilentlyContinue | Remove-Item -Force
Remove-Item (Join-Path $results 'ops.env'), (Join-Path $results 'ops.log'), (Join-Path $results 'web.log') -Force -ErrorAction SilentlyContinue
if (-not (Test-Path '.env.verify')) { Copy-Item '.env.verify.example' '.env.verify'; Write-Host 'Created .env.verify from the example (test-only values).' }
if (-not (Test-Path '.env')) { Write-Host 'Warning: .env is missing; the stack starts without mail settings.' -ForegroundColor Yellow }

$envVerify = @{}
Get-Content '.env.verify' | Where-Object { $_ -match '^\s*[A-Z0-9_]+=' } | ForEach-Object {
  $name, $value = $_ -split '=', 2
  $envVerify[$name.Trim()] = $value.Trim()
}

docker compose down --remove-orphans | Out-Null
docker volume inspect $volume *> $null
if ($LASTEXITCODE -eq 0) {
  $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
  docker run --rm -v "${volume}:/data:ro" -v "${results}:/backup" alpine cp -r /data "/backup/backup-$stamp"
  if ($LASTEXITCODE -ne 0) {
    docker compose up -d --wait web caddy | Out-Null   # put the stack back the way it was
    Fail 'The data backup failed; nothing was reset.'
  }
  docker volume rm $volume | Out-Null
  Write-Host "Data backed up to verify-results\backup-$stamp and reset."
}
Get-ChildItem $results -Directory -Filter 'backup-*' | Sort-Object Name -Descending | Select-Object -Skip $KeepBackups |
  ForEach-Object { docker run --rm -v "$($results):/backup" alpine rm -rf "/backup/$($_.Name)" }

# --- 3. Rebuild and restart -------------------------------------------------------------------------------------
Step 'Rebuild and restart the stack'
docker compose up --build -d --wait web caddy
if ($LASTEXITCODE -ne 0) {
  docker compose logs --tail 40 web
  # Leave a readable result so the failure is not invisible to whoever reads verify-results.
  Add-Content (Join-Path $results 'latest.log') 'FAIL stack        the stack became healthy after the rebuild (0ms) - see docker compose logs web'
  Fail 'The stack did not become healthy.'
}
docker compose logs web --no-color | Out-File -Encoding utf8 (Join-Path $results 'web.log')

# --- 4. Seed and operator commands (answers piped on standard input, never as arguments) --------------------------
Step 'Seed administrators and run the operator commands'
function Run-Operator([string]$command, [string[]]$answers) {
  $output = ($answers -join "`n") + "`n" | docker compose exec -T web dotnet GooseWebsite.Api.dll admin $command 2>&1
  $code = $LASTEXITCODE
  Add-Content (Join-Path $results 'ops.log') "--- admin $command (exit $code)`n$($output -join "`n")"
  return $code
}
$e1 = $envVerify['VERIFY_ADMIN_EMAIL']; $p1 = $envVerify['VERIFY_ADMIN_PASSWORD']
$e2 = $envVerify['VERIFY_ADMIN2_EMAIL']; $p2 = $envVerify['VERIFY_ADMIN2_PASSWORD']; $p2new = $envVerify['VERIFY_ADMIN2_NEW_PASSWORD']
$ops = [ordered]@{
  OPS_PROVISION_EXIT           = Run-Operator 'provision' @($e1, $p1, $p1)
  OPS_PROVISION2_EXIT          = Run-Operator 'provision' @($e2, $p2, $p2)
  OPS_DUPLICATE_PROVISION_EXIT = Run-Operator 'provision' @($e1, $p1, $p1)
  OPS_UNKNOWN_RESET_EXIT       = Run-Operator 'reset-password' @('nobody@example.test', $p2new, $p2new)
  OPS_RESET_EXIT               = Run-Operator 'reset-password' @($e2, $p2new, $p2new)
}
($ops.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) | Set-Content -Encoding ascii (Join-Path $results 'ops.env')
$ops.GetEnumerator() | ForEach-Object { Write-Host ("{0} = {1}" -f $_.Key, $_.Value) }

# --- 5. Verification container ----------------------------------------------------------------------------------
Step 'Unit tests and runtime tests (verification container)'
docker compose --profile verify build verify
if ($LASTEXITCODE -ne 0) { Fail 'The verification image failed to build.' }
docker compose --profile verify run --rm verify
$verifyExit = $LASTEXITCODE

Step 'Result'
if (Test-Path (Join-Path $results 'latest.json')) {
  $summary = Get-Content (Join-Path $results 'latest.json') -Raw | ConvertFrom-Json
  Write-Host "$($summary.passed) passed, $($summary.failed) failed, $($summary.skipped) skipped, $($summary.known) known (defects listed in verify/browser/known-issues.json)"
  Write-Host 'Log:  verify-results\latest.log'
  Write-Host 'JSON: verify-results\latest.json'
} else {
  Write-Host 'The verification container produced no results file.' -ForegroundColor Red
  if ($verifyExit -eq 0) { $verifyExit = 3 }
}
exit $verifyExit
