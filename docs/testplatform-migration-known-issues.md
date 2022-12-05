# Testplatform Migration Known issues

Here are the current known issues you may face when running tests, along with available workarounds.

@# Known Issues

- [Change in Thread.CurrentPrincipal value.](CurrentPrincipal)
- [Change in test execution processes name.](processname)

### <b>Change in Thread.CurrentPrincipal value.</b><a name="CurrentPrincipal"></a>

- <b>Issue:</b> <br/>
Tests that depend on `Thread.CurrentPrincipal` may fail. This is due to change in inter process commincation in Testplatform.
- <b>Workaround:</b> <br/>
Use an alternative like `System.Security.Principal.WindowsIdentity.GetCurrent()`

### <b>Change in test execution processes name.</b><a name="processname"></a>

- <b>Issue:</b> <br/>
Tests that depend on the name of the currnet running process may fail.
- <b>Workaround:</b> <br/>
Tests run in one of following process ```vstest.console.exe```, ```testhost.exe```, ```testhost.x86.exe``` or ```dotnet.exe``` based on run configuration (```/Platform``` and ```/Framework```). If the tests depend on the process name, then update the tests accordingly.
