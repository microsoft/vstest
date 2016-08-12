@IF NOT DEFINED _ECHO @ECHO OFF 

@ECHO. 

SET TPBINRELPATH=..\\..\\artifacts\\src\\Microsoft.TestPlatform.VSIXCreator\\bin\\Release 

IF EXIST "%TPBINRELPATH%" ( 
	IF EXIST "%TPBINRELPATH%\\net461\\win7-x64\\Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.dll" ( 
		MOVE /Y "%TPBINRELPATH%\\net461\\win7-x64\\Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.dll" "%TPBINRELPATH%\\net461\\win7-x64\\Extensions" 
	) 
) 


SET TPBINDEBUGPATH=..\\..\\artifacts\\src\\Microsoft.TestPlatform.VSIXCreator\\bin\\Debug 

IF EXIST "%TPBINDEBUGPATH%" ( 
	IF EXIST "%TPBINDEBUGPATH%\\net461\\win7-x64\\Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.dll" ( 
		MOVE /Y "%TPBINDEBUGPATH%\\net461\\win7-x64\\Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.dll" "%TPBINDEBUGPATH%\\net461\\win7-x64\\Extensions" 
	) 
)