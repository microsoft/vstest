param ()

$TP_ROOT_DIR = (Get-Item (Split-Path $MyInvocation.MyCommand.Path)).Parent.FullName
Write-Host "##vso[task.setvariable variable=DOTNET_ROOT;]$TP_ROOT_DIR\tools\dotnet"
Write-Host "##vso[task.setvariable variable=DOTNET_ROOT(x86);]$TP_ROOT_DIR\tools\dotnet_x86"
Write-Host "##vso[task.setvariable variable=PATH;]$TP_ROOT_DIR\tools\dotnet;$env:PATH"


