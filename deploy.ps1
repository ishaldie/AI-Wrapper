# ZSR Underwriting - Deploy Script
# Run from: Right-click > Run with PowerShell (as Admin)
# Or from admin terminal: powershell -ExecutionPolicy Bypass -File deploy.ps1

param(
    [switch]$SkipPush
)

$ErrorActionPreference = "Stop"
$ProjectRoot = $PSScriptRoot
$PublishDir = "C:\Users\zrina\zsrventures-publish"
$WebProject = "$ProjectRoot\src\ZSR.Underwriting.Web"

# Check admin privileges (needed for iisreset)
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "Restarting as Administrator..." -ForegroundColor Yellow
    Start-Process powershell -ArgumentList "-ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
    exit
}

Write-Host "=== ZSR Underwriting Deploy ===" -ForegroundColor Cyan

# Step 1: Git push
if (-not $SkipPush) {
    Write-Host "`n[1/5] Pushing to GitHub..." -ForegroundColor Yellow
    Set-Location $ProjectRoot
    git add -A
    $hasChanges = git diff --cached --quiet 2>&1; $LASTEXITCODE -ne 0
    if ($hasChanges) {
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm"
        git commit -m "deploy: $timestamp"
    }
    git push origin main
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Git push failed!" -ForegroundColor Red
        Read-Host "Press Enter to exit"
        exit 1
    }
    Write-Host "Git push complete." -ForegroundColor Green
} else {
    Write-Host "`n[1/5] Skipping git push." -ForegroundColor Gray
}

# Step 2: Stop IIS (release file locks)
Write-Host "`n[2/5] Stopping IIS..." -ForegroundColor Yellow
iisreset /stop
if ($LASTEXITCODE -ne 0) {
    Write-Host "IIS stop failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "IIS stopped." -ForegroundColor Green

# Step 3: Publish
Write-Host "`n[3/5] Publishing..." -ForegroundColor Yellow
dotnet publish $WebProject -c Release -o $PublishDir --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed! Restarting IIS..." -ForegroundColor Red
    iisreset /start
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "Publish complete." -ForegroundColor Green

# Step 4: Apply EF migrations to published DB
Write-Host "`n[4/5] Applying database migrations..." -ForegroundColor Yellow
$dbPath = "$PublishDir\zsr_underwriting.db"
if (Test-Path $dbPath) {
    dotnet ef database update `
        --project "$ProjectRoot\src\ZSR.Underwriting.Infrastructure" `
        --startup-project $WebProject `
        --connection "Data Source=$dbPath" `
        --no-build 2>&1 | Out-Null
    Write-Host "Migrations applied." -ForegroundColor Green
} else {
    Write-Host "No database found - copying from dev..." -ForegroundColor Yellow
    Copy-Item "$WebProject\zsr_underwriting.db" $dbPath
    Write-Host "Database copied." -ForegroundColor Green
}

# Step 5: Start IIS
Write-Host "`n[5/5] Starting IIS..." -ForegroundColor Yellow
iisreset /start
if ($LASTEXITCODE -ne 0) {
    Write-Host "IIS start failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "`n=== Deploy Complete ===" -ForegroundColor Green
Write-Host "Site is live at http://zsrventures.com" -ForegroundColor Cyan
Read-Host "`nPress Enter to exit"
