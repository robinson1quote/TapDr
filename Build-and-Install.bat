@echo off
setlocal
title TapDR 5th Axis Stock Generator - Mastercam 2027

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0installer\Install-TapDR-Dovetail.ps1" %*
set "RESULT=%ERRORLEVEL%"

echo.
if "%RESULT%"=="0" (
    echo TapDR build and installation completed.
) else (
    echo TapDR build or installation failed. Check the desktop log for details.
)

pause
exit /b %RESULT%
