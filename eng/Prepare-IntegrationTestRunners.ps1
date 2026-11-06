[CmdletBinding()]
param(
    [string] $Configuration = 'Debug',
    [string] $Version,
    [switch] $SelfTest
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$dotnet = Join-Path $repo '.dotnet\dotnet.exe'
$bin = Join-Path $repo 'artifacts\bin'
$output = Join-Path $repo "artifacts\$Configuration"
$desktop = Join-Path $output 'net451\win7-x64'
$core = Join-Path $output 'netcoreapp2.0'
$coreDesktopHost = Join-Path $core 'TestHost'

function Publish-TestProduct([string] $Project, [string] $Framework, [string] $Destination) {
    Write-Host "Staging $Project ($Framework) -> $Destination"
    & $dotnet publish (Join-Path $repo $Project) --configuration $Configuration --framework $Framework --no-build --no-restore --output $Destination -v:quiet
    if ($LASTEXITCODE -ne 0) {
        throw "SETUP FAILURE: could not stage $Project ($Framework), exit $LASTEXITCODE."
    }
}

function Copy-TestExtension([string] $Project, [string] $Framework, [string] $Assembly, [string] $Destination) {
    $source = Join-Path $bin "$Project\$Configuration\$Framework"
    $extensionDirectory = Join-Path $Destination 'Extensions'
    foreach ($name in @("$Assembly.dll", "$Assembly.pdb")) {
        $path = Join-Path $source $name
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "SETUP FAILURE: extension input not found: $path"
        }
        New-Item -ItemType Directory -Path $extensionDirectory -Force | Out-Null
        Copy-Item -LiteralPath $path -Destination $extensionDirectory -Force
        # A root copy wins assembly resolution. The host provider then looks for testhost
        # above the runner directory rather than above Extensions.
        $rootCopy = Join-Path $Destination $name
        if (Test-Path -LiteralPath $rootCopy -PathType Leaf) {
            Remove-Item -LiteralPath $rootCopy
        }
    }
    foreach ($culture in Get-ChildItem -LiteralPath $source -Directory) {
        $satellite = Join-Path $culture.FullName "$Assembly.resources.dll"
        if (Test-Path -LiteralPath $satellite -PathType Leaf) {
            $target = Join-Path $extensionDirectory $culture.Name
            New-Item -ItemType Directory -Path $target -Force | Out-Null
            Copy-Item -LiteralPath $satellite -Destination $target -Force
        }
    }
}

function Copy-LegacyTestDependencies {
    $packages = $env:NUGET_PACKAGES
    if (-not $packages) {
        $packages = Join-Path $env:USERPROFILE '.nuget\packages'
    }
    $legacy = Join-Path $packages 'microsoft.testplatform\15.9.2\tools\net451\Common7\IDE\Extensions\TestPlatform'
    if (-not (Test-Path -LiteralPath $legacy -PathType Container)) {
        throw "SETUP FAILURE: legacy test dependencies not restored: $legacy"
    }
    $legacy = (Resolve-Path -LiteralPath $legacy).Path
    $script:legacyDependencyRoot = $legacy
    $script:legacyPackageRoot = (Resolve-Path -LiteralPath (Join-Path $packages 'microsoft.testplatform\15.9.2')).Path

    $productNames = @(
        foreach ($project in Get-ChildItem (Join-Path $repo 'src') -Filter '*.csproj' -Recurse -File) {
            [xml] $xml = Get-Content -LiteralPath $project.FullName -Raw
            foreach ($name in $xml.SelectNodes('//*[local-name()="AssemblyName"]')) {
                if ($name.InnerText -and -not $name.InnerText.Contains('$(')) {
                    $name.InnerText
                }
            }
        }
    )
    $copied = 0
    foreach ($file in Get-ChildItem -LiteralPath $legacy -Recurse -File) {
        $owned = @($productNames | Where-Object {
            $file.Name.Equals($_, [StringComparison]::OrdinalIgnoreCase) -or
            $file.Name.StartsWith("$_." , [StringComparison]::OrdinalIgnoreCase)
        }).Count -gt 0
        if ($owned) {
            continue
        }
        $destination = Join-Path $desktop $file.FullName.Substring($legacy.Length + 1)
        # Retain every dependency already selected by the current build's publish graph.
        if (Test-Path -LiteralPath $destination) {
            continue
        }
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destination
        $copied++
    }
    Write-Host "Staged $copied legacy dependency files; no repository-built assembly or existing file was replaced."
}

if ($SelfTest) {
    $root = Join-Path ([IO.Path]::GetTempPath()) ('vstest-legacy-' + [Guid]::NewGuid().ToString('N'))
    $previousPackages = $env:NUGET_PACKAGES
    try {
        $source = Join-Path $root 'microsoft.testplatform\15.9.2\tools\net451\Common7\IDE\Extensions\TestPlatform'
        $desktop = Join-Path $root 'staged'
        New-Item -ItemType Directory -Path (Join-Path $source 'Extensions'), $desktop -Force | Out-Null
        Set-Content (Join-Path $source 'Extensions\Legacy.TestAdapter.dll') 'legacy adapter'
        Set-Content (Join-Path $source 'Microsoft.TestPlatform.CommunicationUtilities.dll') 'old product'
        Set-Content (Join-Path $source 'vstest.console.exe.config') 'old configuration'
        Set-Content (Join-Path $source 'Dependency.dll') 'old dependency'
        Set-Content (Join-Path $desktop 'Dependency.dll') 'current dependency'
        $env:NUGET_PACKAGES = $root.Replace('\', '\\')
        Copy-LegacyTestDependencies
        if ((Get-Content (Join-Path $desktop 'Extensions\Legacy.TestAdapter.dll') -Raw).Trim() -ne 'legacy adapter' -or
            (Get-Content (Join-Path $desktop 'Dependency.dll') -Raw).Trim() -ne 'current dependency' -or
            (Test-Path (Join-Path $desktop 'Microsoft.TestPlatform.CommunicationUtilities.dll')) -or
            (Test-Path (Join-Path $desktop 'vstest.console.exe.config'))) {
            throw 'Legacy staging violated path, product-exclusion or existing-dependency invariants.'
        }
        Write-Host 'Legacy staging self-test passed.'
    }
    finally {
        $env:NUGET_PACKAGES = $previousPackages
        if (Test-Path -LiteralPath $root) {
            Remove-Item -LiteralPath $root -Recurse -Force
        }
    }
    return
}

# The legacy Publish step also stages delivery-only native payloads. Test validation needs
# runnable products, not signing, VSIX creation, feed access or recompilation.
foreach ($layout in @(
    @{ Framework = 'net451'; Destination = $desktop },
    @{ Framework = 'netcoreapp2.0'; Destination = $core }
)) {
    Publish-TestProduct 'src\package\package\package.csproj' $layout.Framework $layout.Destination
    Publish-TestProduct 'src\vstest.console\vstest.console.csproj' $layout.Framework $layout.Destination
    Publish-TestProduct 'src\datacollector\datacollector.csproj' $layout.Framework $layout.Destination
}

Publish-TestProduct 'src\SettingsMigrator\SettingsMigrator.csproj' 'net451' $desktop
foreach ($destination in @($desktop, $coreDesktopHost)) {
    Publish-TestProduct 'src\testhost\testhost.csproj' 'net451' $destination
    Publish-TestProduct 'src\testhost.x86\testhost.x86.csproj' 'net451' $destination
}
Publish-TestProduct 'src\datacollector\datacollector.csproj' 'net451' $coreDesktopHost

foreach ($layout in @(
    @{ Framework = 'net451'; Destination = $desktop },
    @{ Framework = 'netstandard1.5'; Destination = $core }
)) {
    Copy-TestExtension 'Microsoft.TestPlatform.TestHostProvider' $layout.Framework 'Microsoft.TestPlatform.TestHostRuntimeProvider' $layout.Destination
    Copy-TestExtension 'Microsoft.TestPlatform.Extensions.TrxLogger' $layout.Framework 'Microsoft.VisualStudio.TestPlatform.Extensions.Trx.TestLogger' $layout.Destination
    Copy-TestExtension 'Microsoft.TestPlatform.Extensions.BlameDataCollector' $layout.Framework 'Microsoft.TestPlatform.Extensions.BlameDataCollector' $layout.Destination
    Copy-TestExtension 'Microsoft.TestPlatform.Extensions.EventLogCollector' 'net451' 'Microsoft.TestPlatform.Extensions.EventLogCollector' $layout.Destination
}

Copy-LegacyTestDependencies

foreach ($name in @('Microsoft.TestPlatform.PlatformAbstractions.dll', 'Microsoft.TestPlatform.PlatformAbstractions.pdb', 'Microsoft.TestPlatform.PlatformAbstractions.deps.json')) {
    Copy-Item -LiteralPath (Join-Path $bin "Microsoft.TestPlatform.PlatformAbstractions\$Configuration\netcoreapp1.0\$name") -Destination $core -Force
}
foreach ($relative in @('msdia140typelib_clr0200.dll', 'x64\msdia140.dll', 'x64\msdia140.dll.manifest', 'x86\msdia140.dll', 'x86\msdia140.dll.manifest')) {
    $destination = Join-Path $coreDesktopHost $relative
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $legacyDependencyRoot $relative) -Destination $destination -Force
}
Copy-Item -LiteralPath (Join-Path $legacyPackageRoot 'ThirdPartyNotices.txt') -Destination $output -Force

foreach ($console in @((Join-Path $desktop 'vstest.console.exe'), (Join-Path $core 'vstest.console.dll'))) {
    if (-not (Test-Path -LiteralPath $console -PathType Leaf)) {
        throw "SETUP FAILURE: staged console not found: $console"
    }
}
if (-not $Version) {
    $Version = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $desktop 'vstest.console.exe')).ProductVersion.Split('+')[0]
}
if ($Version -notmatch '^15\.9\.3-hotfix-\d+-\d+$') {
    throw "SETUP FAILURE: integration package version does not follow the servicing version rule: $Version"
}
$packageInputs = Join-Path $repo 'artifacts\obj\integration-packages'
New-Item -ItemType Directory -Path $packageInputs -Force | Out-Null
$nuspec = New-Object System.Xml.XmlDocument
$nuspec.PreserveWhitespace = $true
$nuspec.Load((Join-Path $repo 'src\package\nuspec\Microsoft.TestPlatform.Portable.nuspec'))
$nuspec.package.metadata.version = $Version
$nuspecPath = Join-Path $packageInputs 'Microsoft.TestPlatform.Portable.nuspec'
$nuspec.Save($nuspecPath)
$commit = & git -C $repo rev-parse HEAD
if ($LASTEXITCODE -ne 0) {
    throw 'SETUP FAILURE: could not determine the source revision for the test package.'
}
& $dotnet pack (Join-Path $repo 'src\package\package\package.csproj') --configuration $Configuration --no-build --no-restore -p:IsPackable=true -p:IncludeSymbols=false "-p:RepositoryCommit=$commit" "-p:NuspecFile=$nuspecPath" "-p:NuspecBasePath=$output" '-p:NuspecProperties=Runtime=win7-x64' --output (Join-Path $output 'packages') -v:quiet
if ($LASTEXITCODE -ne 0 -or -not (Test-Path (Join-Path $output "packages\Microsoft.TestPlatform.Portable.$Version.nupkg"))) {
    throw 'SETUP FAILURE: the portable package required by acceptance tests was not produced.'
}
Write-Host 'Integration test runners staged from the current build.'
