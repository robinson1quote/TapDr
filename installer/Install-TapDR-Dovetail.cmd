@echo off
setlocal EnableExtensions
title TapDR 5th Axis Stock Generator — Mastercam 2027

set "WORK=%TEMP%\TapDR_Dovetail_Install"
set "ZIP=%WORK%\TapDr-main.zip"
set "SRC=%WORK%\TapDr-main"

echo.
echo ============================================================
echo   TapDR 5th Axis Stock Generator
echo   Mastercam 2027 — one-click build and install
echo ============================================================
echo.

if exist "%WORK%" rmdir /s /q "%WORK%"
mkdir "%WORK%" >nul 2>&1
if errorlevel 1 goto :FAIL

echo Downloading current TapDR source...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ProgressPreference='SilentlyContinue'; Invoke-WebRequest -UseBasicParsing 'https://github.com/robinson1quote/TapDr/archive/refs/heads/main.zip' -OutFile '%ZIP%'; Expand-Archive -LiteralPath '%ZIP%' -DestinationPath '%WORK%' -Force"
if errorlevel 1 goto :FAIL

if not exist "%SRC%\installer\Install-TapDR-Dovetail.ps1" (
  echo ERROR: Installer source was not found in the downloaded repository.
  goto :FAIL
)

echo.
echo Starting build, tests, and installation...
powershell -NoProfile -ExecutionPolicy Bypass -File "%SRC%\installer\Install-TapDR-Dovetail.ps1"
set "RC=%ERRORLEVEL%"

echo.
if not "%RC%"=="0" (
  echo BUILD OR INSTALL FAILED. No new files were installed unless the final install step had completed.
  goto :END
)

echo TapDR Dovetail Stock Generator was installed.
echo Restart Mastercam 2027 and launch the third-party add-in from Home.
goto :END

:FAIL
echo.
echo ERROR: Source download or extraction failed.
set "RC=1"

:END
echo.
echo Press any key to close this window.
pause >nul
exit /b %RC%
