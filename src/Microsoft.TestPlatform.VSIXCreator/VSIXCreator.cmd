@IF NOT DEFINED _ECHO @ECHO OFF

@ECHO.

SET TPBINRELPATH=..\\..\\artifacts\\src\\Microsoft.TestPlatform.VSIXCreator\\bin\\Release

IF EXIST "%TPBINRELPATH%" (
	IF EXIST "%TPBINRELPATH%\\net461\\Microsoft.TestPlatform.VSIXCreator.exe" (
		PUSHD %TPBINRELPATH%\\net461\\
		Microsoft.TestPlatform.VSIXCreator.exe
		POPD
	)
)

SET TPBINDEBUGPATH=..\\..\\artifacts\\src\\Microsoft.TestPlatform.VSIXCreator\\bin\\Debug

IF EXIST "%TPBINDEBUGPATH%" (
	IF EXIST "%TPBINDEBUGPATH%\\net461\\Microsoft.TestPlatform.VSIXCreator.exe" (
		PUSHD %TPBINDEBUGPATH%\\net461\\
		Microsoft.TestPlatform.VSIXCreator.exe
		POPD
	)
)




