@IF NOT DEFINED _ECHO @ECHO OFF

@ECHO.

SET TPBINRELPATH=..\\..\\artifacts\\src\\Microsoft.TestPlatform.VSIXCreator\\bin\\Release

IF EXIST "%TPBINRELPATH%" (
	IF EXIST "%TPBINRELPATH%\\net461\\TestPlatform.vsix" (
		DEL "%TPBINRELPATH%\\net461\\TestPlatform.vsix"
	)
)

SET TPBINDEBUGPATH=..\\..\\artifacts\\src\\Microsoft.TestPlatform.VSIXCreator\\bin\\Debug

IF EXIST "%TPBINDEBUGPATH%" (
	IF EXIST "%TPBINDEBUGPATH%\\net461\\TestPlatform.vsix" (
		DEL "%TPBINDEBUGPATH%\\net461\\TestPlatform.vsix"
	)
)
