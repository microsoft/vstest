@echo off

REM Copyright (c) Microsoft. All rights reserved.

REM Uses -Command rather than main's -File. With -File every argument reaches PowerShell as a
REM string, so any [bool] parameter of eng\build.ps1 fails to bind:
REM   Cannot convert value "System.String" to type "System.Boolean"
REM main never hits this because it does not pass one; this line needs -warnAsError 0, because it
REM builds 2021 source with a current Roslyn. 16.11's original test.cmd also used -Command.

if not defined MSBUILDTERMINALLOGGER set MSBUILDTERMINALLOGGER=off
powershell -ExecutionPolicy ByPass -NoProfile -NoLogo -Command "& '%~dp0eng\build.ps1' -test %*; exit $LastExitCode;"
exit /b %ErrorLevel%
