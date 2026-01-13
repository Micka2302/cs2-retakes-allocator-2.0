@echo off
setlocal

REM Wrapper to run compile.ps1 without typing the ExecutionPolicy flag
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0compile.ps1" %*

endlocal
