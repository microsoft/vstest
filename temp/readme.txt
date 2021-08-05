This needs to be copied into <where vstest.console.exe or vstest.console.dll is>\Extensions\testhost-core

The dlls are coming from the testhost package + the additional deps on Newtonsoft.Json and Nuget.Frameworks, the testhost.deps.json and runtime.config.json are custom.