# Sets variables which are used across the build tasks.

$buildPrefix = $args[0]
if ($args[2].ToLower() -eq "false") {
  $buildSuffix = $args[1]
  $packageVersion = $buildPrefix+"-"+$buildSuffix
} else {
  $packageVersion = $buildPrefix
  $buildSuffix = [string]::Empty
}

Write-Host "##vso[task.setvariable variable=BuildVersionPrefix;]$buildPrefix"
Write-Host "##vso[task.setvariable variable=BuildVersionSuffix;]$buildSuffix"
Write-Host "##vso[task.setvariable variable=PackageVersion;]$packageVersion"

# Set Newtonsoft.Json version to consume in  CI build "Package: TestPlatform SDK" task.
# "Nuget.exe pack" required JsonNetVersion property for creating nuget package.
$env:TP_ROOT_DIR = (Get-Item (Split-Path $MyInvocation.MyCommand.Path)).Parent.FullName
$JsonNetVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.JsonNetVersion
Write-Host "##vso[task.setvariable variable=JsonNetVersion;]$JsonNetVersion"