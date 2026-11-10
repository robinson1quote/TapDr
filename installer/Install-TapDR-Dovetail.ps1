[CmdletBinding()]
param(
    [string]$MastercamInstallDir = "C:\Program Files\Mastercam 2027"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$Solution = Join-Path $RepoRoot "TapDR.Dovetail.sln"
$TestProject = Join-Path $RepoRoot "tests\TapDR.Dovetail.Core.Tests\TapDR.Dovetail.Core.Tests.csproj"
$AdapterProject = Join-Path $RepoRoot "src\TapDR.Dovetail.Mastercam2027\TapDR.Dovetail.Mastercam2027.csproj"
$AdapterOutput = Join-Path $RepoRoot "src\TapDR.Dovetail.Mastercam2027\bin\Release\net10.0-windows"
$InstallDirectory = Join-Path $env:PUBLIC "Documents\Shared Mastercam 2027\Add-Ins\TapDR Dovetail Stock Generator"
$Desktop = [Environment]::GetFolderPath([Environment+SpecialFolder]::DesktopDirectory)
$LogPath = Join-Path $Desktop ("TapDR_Dovetail_Build_" + (Get-Date -Format "yyyyMMdd_HHmmss") + ".log")
Start-Transcript -Path $LogPath -Force | Out-Null

try {
    Write-Host ""
    Write-Host "TapDR 5th Axis Stock Generator — Mastercam 2027" -ForegroundColor Cyan
    Write-Host "Checking build prerequisites..."

    if (-not (Test-Path (Join-Path $MastercamInstallDir "NETHook3_0.dll"))) {
        throw "Mastercam 2027 NET-Hook SDK was not found at: $MastercamInstallDir"
    }
    if (-not (Test-Path (Join-Path $MastercamInstallDir "ModelessDialogTabHandler.dll"))) {
        throw "Mastercam ModelessDialogTabHandler.dll was not found."
    }
    if (-not (Test-Path (Join-Path $MastercamInstallDir "MCGeomSld.dll"))) {
        throw "Mastercam MCGeomSld.dll was not found. The native solid runtime is incomplete."
    }
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw ".NET SDK command 'dotnet' was not found."
    }
    $sdkList = & dotnet --list-sdks
    if ($LASTEXITCODE -ne 0 -or -not ($sdkList -match '(^|\s)10\.')) {
        throw ".NET 10 SDK is required. Installed SDKs: $($sdkList -join '; ')"
    }
    if (-not (Test-Path $Solution)) { throw "Solution not found: $Solution" }
    if (-not (Test-Path (Join-Path $RepoRoot "src\TapDR.Dovetail.Mastercam2027\TapDR.Dovetail.Mastercam2027.ft"))) {
        throw "Mastercam function table is missing."
    }

    Write-Host ""
    Write-Host "[1/3] Running geometry-core tests..." -ForegroundColor Yellow
    & dotnet test $TestProject --configuration Release --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw "Geometry-core tests failed. Nothing was installed." }

    Write-Host ""
    Write-Host "[2/3] Building Mastercam 2027 NET-Hook..." -ForegroundColor Yellow
    & dotnet build $AdapterProject --configuration Release -p:MastercamInstallDir="$MastercamInstallDir" --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw "Mastercam add-in build failed. Nothing was installed." }

    $AdapterDll = Join-Path $AdapterOutput "TapDR.Dovetail.Mastercam2027.dll"
    $CoreDll = Join-Path $AdapterOutput "TapDR.Dovetail.Core.dll"
    if (-not (Test-Path $AdapterDll)) { throw "Build output missing: $AdapterDll" }
    if (-not (Test-Path $CoreDll)) { throw "Build output missing: $CoreDll" }

    Write-Host ""
    Write-Host "[3/3] Installing into Shared Mastercam 2027 Add-Ins..." -ForegroundColor Yellow
    if (Test-Path $InstallDirectory) {
        $BackupRoot = Join-Path (Split-Path -Parent $InstallDirectory) "TapDR Dovetail Backups"
        New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null
        $BackupPath = Join-Path $BackupRoot (Get-Date -Format "yyyyMMdd_HHmmss")
        Copy-Item -LiteralPath $InstallDirectory -Destination $BackupPath -Recurse -Force
        Write-Host "Previous installation backed up to: $BackupPath"
    }

    New-Item -ItemType Directory -Force -Path $InstallDirectory | Out-Null
    Copy-Item -LiteralPath $AdapterDll -Destination $InstallDirectory -Force
    Copy-Item -LiteralPath $CoreDll -Destination $InstallDirectory -Force
    Copy-Item -LiteralPath (Join-Path $RepoRoot "src\TapDR.Dovetail.Mastercam2027\TapDR.Dovetail.Mastercam2027.ft") -Destination $InstallDirectory -Force

    $Readme = @'
TapDR 5th Axis Stock Generator — Mastercam 2027

Installed files:
- TapDR.Dovetail.Mastercam2027.dll
- TapDR.Dovetail.Core.dll
- TapDR.Dovetail.Mastercam2027.ft

Launch:
1. Restart Mastercam 2027.
2. Open Home > Run Add-In > Run Third-Party Add-In.
3. Select TapDR.Dovetail.Mastercam2027.dll from this folder.

The add-in currently generates the DSP-V vise stock-prep profile for V75100X,
original V562, and V6105. Fixture stock-prep profiles remain disabled until
their drawing coordinates are verified.

The geometry core tests ran successfully before installation. This installer
does not claim that the add-in has been runtime-verified in Mastercam.
'@
    Set-Content -LiteralPath (Join-Path $InstallDirectory "README.txt") -Value $Readme -Encoding UTF8

    Write-Host ""
    Write-Host "INSTALLATION COMPLETE" -ForegroundColor Green
    Write-Host "Installed to: $InstallDirectory"
    Write-Host "Build/test log: $LogPath"
    Write-Host "Restart Mastercam 2027 and launch from Home > Run Add-In > Run Third-Party Add-In."
}
catch {
    Write-Host ""
    Write-Host "BUILD / INSTALL FAILED" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($StagingDirectory -and (Test-Path $StagingDirectory)) {
        Remove-Item -LiteralPath $StagingDirectory -Recurse -Force -ErrorAction SilentlyContinue
    }
    if ($BackupPath -and (Test-Path $BackupPath) -and -not (Test-Path $InstallDirectory)) {
        Move-Item -LiteralPath $BackupPath -Destination $InstallDirectory -ErrorAction SilentlyContinue
    }
    Write-Host "Log: $LogPath"
    exit 1
}
finally {
    Stop-Transcript | Out-Null
}
