[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. $PSScriptRoot\common\tools.ps1

$version = $GlobalJson.tools.dotnet
$x86Root = Join-Path $RepoRoot '.dotnet\x86'
if (-not (Test-Path -LiteralPath (Join-Path $x86Root "sdk\$version"))) {
    InstallDotNetSdk $x86Root $version 'x86' -noPath
}

if (-not (Test-Path -LiteralPath (Join-Path $x86Root "sdk\$version"))) {
    throw "The pinned x86 SDK $version was not installed in $x86Root."
}
