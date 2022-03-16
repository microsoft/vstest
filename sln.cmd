@echo off

REM Copyright (c) Microsoft. All rights reserved.

REM set DOTNET_ROOT to point at the locally installed dotnet, 
REM to avoid problems with .NET Core 2.1 that we run our tests and tools against,
REM but that is regularly corrupted.

set DOTNET_ROOT=%~dp0tools\dotnet
set DOTNET_ROOT(x86)=%~dp0tools\dotnet_x86

start %~dp0TestPlatform.sln

if %errorlevel% neq 0 exit /b %errorlevel% 
