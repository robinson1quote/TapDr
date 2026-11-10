[CmdletBinding()]
param(
    [string]$MastercamInstallDir = "C:\Program Files\Mastercam 2027"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# This script lives in <repo>\installer; PSScriptRoot is the installer folder.
$RepoRoot = Split-Path -Parent $PSScriptRoot
$Solution = Join-Path $RepoRoot "TapDR.Dovetail.sln"
$TestProject = Join-Path $RepoRoot "tests\TapDR.Dovetail.Core.Tests\TapDR.Dovetail.Core.Tests.csproj"
$AdapterProject = Join-Path $RepoRoot "src\TapDR.Dovetail.Mastercam2027\TapDR.Dovetail.Mastercam2027.csproj"
$AdapterOutput = Join-Path $RepoRoot "src\TapDR.Dovetail.Mastercam2027\bin\Release\net10.0-windows"
$InstallDirectory = Join-Path $env:PUBLIC "Documents\Shared Mastercam 2027\Add-Ins\TapDR Dovetail Stock Generator"
$InstallParent = Split-Path -Parent $InstallDirectory
$BackupRoot = Join-Path $InstallParent "TapDR Dovetail Backups"
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$StagingDirectory = Join-Path $InstallParent ("TapDR Dovetail Staging " + $Timestamp)
$BackupPath = Join-Path $BackupRoot $Timestamp
$Desktop = [Environment]::GetFolderPath([Environment+SpecialFolder]::DesktopDirectory)
$LogPath = Join-Path $Desktop ("TapDR_Dovetail_Build_" + $Timestamp + ".log")
$TranscriptStarted = $false
$PreviousInstallMoved = $false

try {
    Start-Transcript -Path $LogPath -Force | Out-Null
    $TranscriptStarted = $true

    Write-Host ""
    Write-Host "TapDR 5th Axis Stock Generator — Mastercam 2027" -ForegroundColor Cyan
    Write-Host "Checking build prerequisites..."

    foreach ($requiredFile in @("NETHook3_0.dll", "MCGeomSld.dll")) {
        $path = Join-Path $MastercamInstallDir $requiredFile
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Required Mastercam 2027 file was not found: $path"
        }
    }
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw ".NET SDK command 'dotnet' was not found."
    }
    $sdkList = & dotnet --list-sdks
    if ($LASTEXITCODE -ne 0 -or -not ($sdkList -match '(^|\s)10\.')) {
        throw ".NET 10 SDK is required. Installed SDKs: $($sdkList -join '; ')"
    }
    foreach ($requiredPath in @($Solution, $TestProject, $AdapterProject,
        (Join-Path $RepoRoot "src\TapDR.Dovetail.Mastercam2027\TapDR.Dovetail.Mastercam2027.ft"))) {
        if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
            throw "Required project file was not found: $requiredPath"
        }
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
    foreach ($requiredOutput in @($AdapterDll, $CoreDll)) {
        if (-not (Test-Path -LiteralPath $requiredOutput -PathType Leaf)) {
            throw "Build output missing: $requiredOutput"
        }
    }

    Write-Host ""
    Write-Host "[3/3] Staging the verified build..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Force -Path $StagingDirectory | Out-Null
    Copy-Item -LiteralPath $AdapterDll -Destination $StagingDirectory -Force
    Copy-Item -LiteralPath $CoreDll -Destination $StagingDirectory -Force
    Copy-Item -LiteralPath (Join-Path $RepoRoot "src\TapDR.Dovetail.Mastercam2027\TapDR.Dovetail.Mastercam2027.ft") -Destination $StagingDirectory -Force

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

The current generator creates the DSP-V vise stock-prep solid for V75100X,
original V562, and V6105. D115, D22, and D05125 remain separate fixture
stock-prep profiles and are not yet enabled for solid generation.

The installer runs the geometry-core tests and stops without replacing an
existing installation if the tests or add-in build fail. A successful build
does not by itself prove runtime operation inside Mastercam.
'@
    Set-Content -LiteralPath (Join-Path $StagingDirectory "README.txt") -Value $Readme -Encoding UTF8

    foreach ($fileName in @("TapDR.Dovetail.Mastercam2027.dll", "TapDR.Dovetail.Core.dll", "TapDR.Dovetail.Mastercam2027.ft", "README.txt")) {
        if (-not (Test-Path -LiteralPath (Join-Path $StagingDirectory $fileName) -PathType Leaf)) {
            throw "Staging verification failed; missing $fileName."
        }
    }

    if (Test-Path -LiteralPath $InstallDirectory) {
        New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null
        Move-Item -LiteralPath $InstallDirectory -Destination $BackupPath
        $PreviousInstallMoved = $true
    }

    try {
        Move-Item -LiteralPath $StagingDirectory -Destination $InstallDirectory
    }
    catch {
        if ($PreviousInstallMoved -and (Test-Path -LiteralPath $BackupPath) -and
            -not (Test-Path -LiteralPath $InstallDirectory)) {
            Move-Item -LiteralPath $BackupPath -Destination $InstallDirectory
            $PreviousInstallMoved = $false
        }
        throw
    }

    Write-Host ""
    Write-Host "INSTALLATION COMPLETE" -ForegroundColor Green
    Write-Host "Installed to: $InstallDirectory"
    if ($PreviousInstallMoved) {
        Write-Host "Previous installation backed up to: $BackupPath"
    }
    Write-Host "Build/test log: $LogPath"
    Write-Host "Restart Mastercam 2027 and launch from Home > Run Add-In > Run Third-Party Add-In."
}
catch {
    Write-Host ""
    Write-Host "BUILD / INSTALL FAILED" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red

    if ($PreviousInstallMoved -and (Test-Path -LiteralPath $BackupPath) -and
        -not (Test-Path -LiteralPath $InstallDirectory)) {
        Move-Item -LiteralPath $BackupPath -Destination $InstallDirectory -ErrorAction SilentlyContinue
    }

    Write-Host "Log: $LogPath"
    exit 1
}
finally {
    if (Test-Path -LiteralPath $StagingDirectory) {
        Remove-Item -LiteralPath $StagingDirectory -Recurse -Force -ErrorAction SilentlyContinue
    }
    if ($TranscriptStarted) {
        Stop-Transcript | Out-Null
    }
}
