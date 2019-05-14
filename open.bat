REM #############################################################
REM ## This is used to open the sln file (with default program ##
REM ## associated with sln files) with PATH overidden to point ##
REM ## to the dotnet installation in the tools folder.         ##
REM #############################################################
set PATH=%cd%\tools\dotnet;%PATH%
TestPlatform.sln