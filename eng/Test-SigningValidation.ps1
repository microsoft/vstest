# Exercises the real SDK projects without signing or publishing anything.
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$restore = $true
$ci = $false
$configuration = 'Debug'
$msbuildEngine = 'dotnet'
$disableConfigureToolsetImport = $true
. $PSScriptRoot\common\tools.ps1

$dotnet = Join-Path (InitializeDotNetCli $true) (GetExecutableFileName 'dotnet')
$root = Join-Path $TempDir ("signing-validation-test-" + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $root

try {
    $fixture = Join-Path $root 'fixture'
    $packages = Join-Path $root 'PackageArtifacts'
    $empty = Join-Path $root 'empty'
    $partial = Join-Path $root 'partial'
    $null = New-Item -ItemType Directory -Path $fixture, $packages, $empty, $partial
    foreach ($file in @('Directory.Build.props', 'Directory.Build.targets')) {
        [IO.File]::WriteAllText((Join-Path $fixture $file), '<Project />')
    }
    $framework = 'net' + ([version]($GlobalJson.tools.dotnet.Split('-')[0])).Major + '.0'
    $fixtureProject = Join-Path $fixture 'UnsignedFixture.csproj'
    [IO.File]::WriteAllText($fixtureProject, @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>$framework</TargetFramework>
    <PackageId>UnsignedFixture</PackageId>
    <Version>1.0.0</Version>
  </PropertyGroup>
</Project>
"@)
    [IO.File]::WriteAllText((Join-Path $fixture 'Fixture.cs'), 'public sealed class UnsignedFixture { }')
    & $dotnet pack $fixtureProject -nologo -verbosity:quiet -o $packages
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not build the valid unsigned package fixture.'
    }
    $nupkg = Join-Path $packages 'UnsignedFixture.1.0.0.nupkg'
    if (-not (Test-Path -LiteralPath $nupkg)) {
        throw 'The unsigned package fixture was not produced.'
    }
    Write-Host 'Built UnsignedFixture.1.0.0.nupkg containing UnsignedFixture.dll.'

    $signProject = GetSdkTaskProject 'Sign'
    # An invalid signing mode is a safety stop if the producer guard regresses.
    # The guard must fail first; SignToolTask must never execute in this test.
    $signArguments = @(
        'msbuild', $signProject, '-nologo', '-t:Execute', '-verbosity:minimal',
        "/p:RepoRoot=$RepoRoot", '/p:Configuration=Release', '/p:DotNetSignType=InvalidSigningProbe',
        "/p:_ServicingVsixOutDir=$empty\",
        "/p:BaseIntermediateOutputPath=$root\Sign\"
    )
    $output = (& $dotnet @signArguments "/p:_ServicingPackageOutDir=$empty\" 2>&1) -join "`n"
    if ($LASTEXITCODE -eq 0 -or $output -notmatch 'No packages resolved into ItemsToSign') {
        throw "Real Sign.proj did not reject the empty producer package set:`n$output"
    }
    Write-Host 'Sign.proj: empty producer package set rejected before Execute.'

    foreach ($id in @(
        'Microsoft.CodeCoverage', 'Microsoft.NET.Test.Sdk', 'Microsoft.TestPlatform.AdapterUtilities',
        'Microsoft.TestPlatform.Build', 'Microsoft.TestPlatform.CLI', 'Microsoft.TestPlatform.Extensions.TrxLogger',
        'Microsoft.TestPlatform.Internal.Uwp', 'Microsoft.TestPlatform.ObjectModel', 'Microsoft.TestPlatform.Portable',
        'Microsoft.TestPlatform.TestHost', 'Microsoft.TestPlatform.TranslationLayer'
    )) {
        Copy-Item -LiteralPath $nupkg -Destination (Join-Path $partial "$id.1.0.0.nupkg")
    }
    $output = (& $dotnet @signArguments "/p:_ServicingPackageOutDir=$partial\" 2>&1) -join "`n"
    if ($LASTEXITCODE -eq 0 -or $output -notmatch 'Expected packages did NOT reach the signing set: Microsoft\.TestPlatform\. Resolved 11 of 12') {
        throw "Real Sign.proj did not reject the incomplete producer package set:`n$output"
    }
    Write-Host 'Sign.proj: missing Microsoft.TestPlatform rejected with the other eleven packages present.'

    Copy-Item -LiteralPath $nupkg -Destination (Join-Path $partial 'Microsoft.TestPlatform.1.0.0.nupkg')
    $output = (& $dotnet @signArguments "/p:_ServicingPackageOutDir=$partial\" 2>&1) -join "`n"
    if ($LASTEXITCODE -eq 0 -or $output -notmatch "The value of DotNetSignType is invalid: 'InvalidSigningProbe'" -or
        $output -notmatch 'Signing set resolved.*12 packages') {
        throw "Real Sign.proj did not accept the complete package set before the safety stop:`n$output"
    }
    Write-Host 'Sign.proj: all twelve package names accepted; invalid signing mode stopped execution before SignToolTask.'

    $validationProject = GetSdkTaskProject 'SigningValidation'
    $validationArguments = @(
        'msbuild', $validationProject, '-nologo', '-verbosity:minimal',
        "/p:RepoRoot=$RepoRoot", '/p:Configuration=Debug',
        "/p:BaseIntermediateOutputPath=$root\SigningValidation\",
        "/p:PackageBasePath=$packages",
        "/p:_ServicingPackageOutDir=$empty\",
        "/p:_ServicingVsixOutDir=$empty\",
        "/p:SignCheckExclusionsFile=$PSScriptRoot\SignCheckExclusionsFile.txt",
        "/p:SignCheckLog=$root\signcheck.log",
        "/p:SignCheckErrorLog=$root\signcheck.errors.log",
        "/p:SignCheckResultsXmlFile=$root\signcheck.xml"
    )
    & $dotnet @validationArguments -t:Restore
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not restore the pinned SigningValidation task.'
    }
    $output = (& $dotnet @validationArguments -t:Execute 2>&1) -join "`n"
    if ($LASTEXITCODE -eq 0 -or $output -notmatch 'SignCheck reported signing issues' -or
        $output -match 'No packages resolved into ItemsToSign|Expected packages did NOT reach the signing set') {
        throw "Standalone validation did not reach the real signature check:`n$output"
    }
    [xml]$report = Get-Content -LiteralPath (Join-Path $root 'signcheck.xml') -Raw
    $files = @($report.SelectNodes('//File'))
    foreach ($name in @('UnsignedFixture.1.0.0.nupkg', 'UnsignedFixture.dll')) {
        $matches = @($files | Where-Object { $_.Name -like "*$name" -and $_.Outcome -eq 'Unsigned' })
        if ($matches.Count -ne 1 -or $matches[0].HasAttribute('Error')) {
            throw "SignCheck did not inspect $name as a valid unsigned file."
        }
    }
    $packageResults = @($report.SignCheckResults.File)
    if ($packageResults.Count -ne 1 -or $packageResults[0].Name -notlike '*UnsignedFixture.1.0.0.nupkg' -or
        @($packageResults[0].SelectNodes('.//File') | Where-Object { $_.Name -like '*UnsignedFixture.dll' }).Count -ne 1) {
        throw 'SignCheck did not recursively inspect the fixture package supplied through PackageBasePath.'
    }
    Write-Host "SigningValidation.proj: inspected $($files.Count) files in PackageBasePath and rejected the unsigned package and its assembly."
    $files | Where-Object { $_.Outcome -eq 'Unsigned' } | ForEach-Object {
        Write-Host "Unsigned: $($_.Name)"
    }
}
finally {
    Remove-Item -LiteralPath $root -Recurse -Force
}
