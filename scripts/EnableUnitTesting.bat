
@IF NOT DEFINED _ECHO @ECHO OFF
 
@ECHO.
@ECHO Enabling Unit Testing

PUSHD "%~dp0"

setlocal enableextensions disabledelayedexpansion

    set "search="outputName"
    set "replace=//"outputName"

    set "textFile="..\src\Microsoft.TestPlatform.ObjectModel\project.json""

    for /f "delims=" %%i in ('type "%textFile%" ^& break ^> "%textFile%" ') do (
        set "line=%%i"
        setlocal enabledelayedexpansion
        set "line=!line:%search%=%replace%!"
        >>"%textFile%" echo(!line!
        endlocal
    )