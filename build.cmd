@echo off

REM Copyright (c) Microsoft. All rights reserved.

REM Uses -Command rather than main's -File: with -File every argument reaches PowerShell as a
REM string, so any [bool] parameter of eng\build.ps1 fails to bind. See test.cmd for the detail.

if not defined MSBUILDTERMINALLOGGER set MSBUILDTERMINALLOGGER=off
powershell -ExecutionPolicy ByPass -NoProfile -NoLogo -Command "& '%~dp0eng\build.ps1' %*; exit $LastExitCode;"
exit /b %ErrorLevel%
