@echo off
if not defined MSBUILDTERMINALLOGGER set MSBUILDTERMINALLOGGER=off
powershell -ExecutionPolicy ByPass -NoProfile -File "%~dp0eng\Build.ps1" -test %*
exit /b %ErrorLevel%
