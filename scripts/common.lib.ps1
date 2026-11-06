# Copyright (c) Microsoft. All rights reserved.

$ErrorActionPreference = "Stop"
$script:ScriptFailedCommands = @()
$script:ScriptFailed = $false

#
# Git Branch
#
$TPB_BRANCH = "LOCALBRANCH"
$TPB_COMMIT = "LOCALBUILD"

try {
    $TPB_BRANCH = $env:BUILD_SOURCEBRANCH -replace "^refs/heads/"
    if ([string]::IsNullOrWhiteSpace($TPB_BRANCH)) {
        $TPB_BRANCH = git -C "." rev-parse --abbrev-ref HEAD
    }
}
catch { }

try {
    $TPB_COMMIT = $env:BUILD_SOURCEVERSION
    if ([string]::IsNullOrWhiteSpace($TPB_COMMIT)) {
        $TPB_COMMIT = git -C "." rev-parse HEAD
    }
}
catch { }

#
# Variables
#
Write-Verbose "Setup environment variables."
$CurrentScriptDir = (Get-Item (Split-Path $MyInvocation.MyCommand.Path))
$env:TP_ROOT_DIR = $CurrentScriptDir.Parent.FullName
$env:TP_TOOLS_DIR = Join-Path $env:TP_ROOT_DIR "tools"
$env:TP_PACKAGES_DIR = Join-Path $env:TP_ROOT_DIR "packages"
$env:TP_OUT_DIR = Join-Path $env:TP_ROOT_DIR "artifacts"
$env:TP_TESTARTIFACTS = Join-Path $env:TP_OUT_DIR "testArtifacts"
$env:TP_PACKAGE_PROJ_DIR = Join-Path $env:TP_ROOT_DIR "src\package"
$GlobalJson = Get-Content -Raw -Path (Join-Path $env:TP_ROOT_DIR 'global.json') | ConvertFrom-Json

#
# Dotnet configuration
#
# Disable first run since we want to control all package sources
Write-Verbose "Setup dotnet configuration."
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1
# Dotnet build doesn't support --packages yet. See https://github.com/dotnet/cli/issues/2712
$env:NUGET_PACKAGES = $env:TP_PACKAGES_DIR
$env:NUGET_EXE_Version = "6.0.0"
$env:DOTNET_CLI_VERSION = $GlobalJson.tools.dotnet
# $env:DOTNET_RUNTIME_VERSION = "LATEST"
$env:VSWHERE_VERSION = "2.0.2"
$env:MSBUILD_VERSION = "15.0"

function Write-Log {
    param (
        [string] $message,
        [ValidateSet("Success", "Error")]
        [string]
        $Level = "Success"
    )

    if ($message)
    {
        $color = if ("Success" -eq $Level) { "Green" } else { "Red" }
        Write-Host "... $message" -ForegroundColor $color
    }
}

function Write-VerboseLog([string] $message)
{
    Write-Verbose $message
}

function Remove-Tools
{
}

function Install-DotNetCli
{
    $timer = Start-Timer
    Write-Log "Install-DotNetCli: Get dotnet-install.ps1 script..."
    $dotnetInstallRemoteScript = "https://dot.net/v1/dotnet-install.ps1"
    $dotnetInstallScript = Join-Path $env:TP_TOOLS_DIR "dotnet-install.ps1"
    if (-not (Test-Path $env:TP_TOOLS_DIR)) {
        New-Item $env:TP_TOOLS_DIR -Type Directory | Out-Null
    }

    $dotnet_dir= Join-Path $env:TP_TOOLS_DIR "dotnet"

    if (-not (Test-Path $dotnet_dir)) {
        New-Item $dotnet_dir -Type Directory | Out-Null
    }

    (New-Object System.Net.WebClient).DownloadFile($dotnetInstallRemoteScript, $dotnetInstallScript)

    if (-not (Test-Path $dotnetInstallScript)) {
        Write-Error "Failed to download dotnet install script."
    }

    Unblock-File $dotnetInstallScript

    Write-Log "Install-DotNetCli: Get the latest dotnet cli toolset..."
    $dotnetInstallPath = Join-Path $env:TP_TOOLS_DIR "dotnet"
    New-Item -ItemType directory -Path $dotnetInstallPath -Force | Out-Null

    & $dotnetInstallScript -InstallDir "$dotnetInstallPath" -Runtime 'dotnet' -Channel '2.1' -Architecture x64 -NoPath -Version '2.1.30'
    & $dotnetInstallScript -InstallDir "$dotnetInstallPath" -Runtime 'dotnet' -Channel '3.1' -Architecture x64 -NoPath -Version '3.1.24'
    & $dotnetInstallScript -InstallDir "$dotnetInstallPath" -Runtime 'dotnet' -Channel '5.0' -Architecture x64 -NoPath -Version '5.0.16'
    & $dotnetInstallScript -InstallDir "$dotnetInstallPath" -Runtime 'dotnet' -Channel '6.0' -Architecture x64 -NoPath -Version '6.0.4'
    & $dotnetInstallScript -InstallDir "$dotnetInstallPath" -Channel '7.0' -Architecture x64 -NoPath -Version $env:DOTNET_CLI_VERSION

    & $dotnetInstallScript -InstallDir "${dotnetInstallPath}_x86" -Runtime 'dotnet' -Channel '2.1' -Architecture x86 -NoPath -Version '2.1.30'
    & $dotnetInstallScript -InstallDir "${dotnetInstallPath}_x86" -Runtime 'dotnet' -Channel '3.1' -Architecture x86 -NoPath -Version '3.1.24'
    & $dotnetInstallScript -InstallDir "${dotnetInstallPath}_x86" -Runtime 'dotnet' -Channel '5.0' -Architecture x86 -NoPath -Version '5.0.16'
    & $dotnetInstallScript -InstallDir "${dotnetInstallPath}_x86" -Runtime 'dotnet' -Channel '6.0' -Architecture x86 -NoPath -Version '6.0.4'
    & $dotnetInstallScript -InstallDir "${dotnetInstallPath}_x86" -Channel '7.0' -Architecture x86 -NoPath -Version $env:DOTNET_CLI_VERSION

    $env:DOTNET_ROOT= $dotnetInstallPath
    ${env:DOTNET_ROOT(x86)} = "${dotnetInstallPath}_x86"

    $env:DOTNET_MULTILEVEL_LOOKUP=0

    "---- dotnet environment variables"
    Get-ChildItem "Env:\dotnet_*"

    "`n`n---- x64 dotnet"
    Invoke-Exe "$env:DOTNET_ROOT\dotnet.exe" -Arguments "--info"

    "`n`n---- x86 dotnet"
    # avoid erroring out because we don't have the sdk for x86 that global.json requires
    try {
        (Invoke-Exe "${env:DOTNET_ROOT(x86)}\dotnet.exe" -Arguments "--info") 2> $null
    } catch {}
    Write-Log "Install-DotNetCli: Complete. {$(Get-ElapsedTime($timer))}"
}

function Clear-Package {
    # find all microsoft packages that have the same version as we specified
    # this is cache-busting the nuget packages, so we don't reuse them from cache
    # after we built new ones
    if (Test-Path $env:TP_PACKAGES_DIR) {
        $devPackages = Get-ChildItem $env:TP_PACKAGES_DIR/microsoft.*/$TPB_Version | Select-Object -ExpandProperty FullName
        $devPackages | Remove-Item -Force -Recurse -Confirm:$false
    }
}

function Restore-Package
{
    $timer = Start-Timer
    Write-Log "Restore-Package: Start restoring packages to $env:TP_PACKAGES_DIR."
    $dotnetExe = Get-DotNetPath
    Invoke-Exe $dotnetExe -Arguments "restore $env:TP_ROOT_DIR\src\package\external\external.csproj --packages $env:TP_PACKAGES_DIR -v:minimal -warnaserror -p:Version=$TPB_Version -bl:""$env:TP_OUT_DIR\log\$Configuration\external.binlog"""
    Write-Log ".. .. Restore-Package: Complete."
    Write-Log "Restore-Package: Complete. {$(Get-ElapsedTime($timer))}"
}

function Restore-FakesPayload
{
    # Recovers the seven Fakes payload files that Microsoft.TestPlatform.nuspec references through
    # $FakesPackageDir$, and that no feed can supply.
    #
    # WHAT IS ACTUALLY WRONG, because the symptom points somewhere else
    #
    # Microsoft.QualityTools.Testing.Fakes.TestRunnerHarness 17.0.0-beta.21179.1 is not obtainable.
    # Restore substitutes 17.4.0-beta.22405.1, which lands in a directory named after ITS version, so
    # the path built from the PINNED version contains nothing. Restore SUCCEEDS - the NU1603 it raises
    # is suppressed in external.csproj on purpose - and packing fails much later on a missing file.
    # Nothing was wrong with the restore: the thing the build depends on was never something restore
    # produced. Measured on build 3067050, where Create-NugetPackages packed three nupkgs and then
    # died on the fourth with "Could not find a part of the path ...\17.0.0-beta.21179.1\contentFiles".
    #
    # WHERE THE FILES COME FROM
    #
    # All seven ship inside Microsoft.TestPlatform 17.3.0 - the previously released package for this
    # line - under tools\net451\Common7\IDE\Extensions\TestPlatform. The mapping below is taken from
    # the <file> elements of src\package\nuspec\Microsoft.TestPlatform.nuspec, so source and
    # destination are the same seven the nuspec asks for, not a similar-looking set.
    #
    # THE HASHES ARE 17.3.0'S OWN AND WERE MEASURED, NOT COPIED FROM 16.11
    #
    # 16.11 carries the same function against ITS shipped package. Four of the seven files differ
    # between the two releases (the DataCollector, both Instrumentation DLLs and the Fakes DLL), so
    # copying 16.11's recorded hashes would have thrown on four of seven. Each value below was
    # computed from the extracted Microsoft.TestPlatform 17.3.0 package.
    #
    # WHY THE HASH CHECK IS NOT OPTIONAL
    #
    # Today the failure is loud and names the package, the version and the path. An extraction that
    # copied the wrong files, or none, would replace that with a package that is quietly short -
    # trading a loud failure for a silent substitution, which is worse than doing nothing. So every
    # file is verified by SHA-256 against the recorded manifest before it is used, and both
    # "extracted the wrong thing" and "extracted nothing" fail by name.

    $timer = Start-Timer
    Write-Log "Restore-FakesPayload: Started."

    $fakesVersion = ([xml](Get-Content $env:TP_ROOT_DIR\scripts\build\TestPlatform.Dependencies.props)).Project.PropertyGroup.MicrosoftFakesVersion
    $destRoot = Join-Path $env:TP_PACKAGES_DIR "Microsoft.QualityTools.Testing.Fakes.TestRunnerHarness\$fakesVersion\contentFiles"

    # path under $destRoot -> path under the shipped package's TestPlatform extension folder
    $map = [ordered]@{
        "DataCollector\Microsoft.Cci.dll"                                              = "Extensions\Microsoft.Cci.dll"
        "DataCollector\Microsoft.VisualStudio.Fakes.DataCollector.dll"                 = "Extensions\Microsoft.VisualStudio.Fakes.DataCollector.dll"
        "Instrumentation\x64\FakesInstrumentationProfiler_x64.config"                  = "Extensions\Fakes\x64\FakesInstrumentationProfiler_x64.config"
        "Instrumentation\x64\Microsoft.QualityTools.Testing.Fakes.Instrumentation.dll" = "Extensions\Fakes\x64\Microsoft.QualityTools.Testing.Fakes.Instrumentation.dll"
        "Instrumentation\x86\FakesInstrumentationProfiler_x86.config"                  = "Extensions\Fakes\x86\FakesInstrumentationProfiler_x86.config"
        "Instrumentation\x86\Microsoft.QualityTools.Testing.Fakes.Instrumentation.dll" = "Extensions\Fakes\x86\Microsoft.QualityTools.Testing.Fakes.Instrumentation.dll"
        "TestPlatform\Microsoft.VisualStudio.TestPlatform.Fakes.dll"                   = "Microsoft.VisualStudio.TestPlatform.Fakes.dll"
    }

    # SHA-256 prefix of each file as it exists in the shipped 17.3.0 package. Recorded here rather
    # than read from a file beside the repo, so the check travels with the branch and a build agent
    # needs nothing but the package.
    $expected = @{
        "DataCollector\Microsoft.Cci.dll"                                              = "7207CAE4EFAA"
        "DataCollector\Microsoft.VisualStudio.Fakes.DataCollector.dll"                 = "A003AD100B14"
        "Instrumentation\x64\FakesInstrumentationProfiler_x64.config"                  = "ECB5907F7C96"
        "Instrumentation\x64\Microsoft.QualityTools.Testing.Fakes.Instrumentation.dll" = "96F2F54812BA"
        "Instrumentation\x86\FakesInstrumentationProfiler_x86.config"                  = "9B178940E4E3"
        "Instrumentation\x86\Microsoft.QualityTools.Testing.Fakes.Instrumentation.dll" = "E04EC7DAAE94"
        "TestPlatform\Microsoft.VisualStudio.TestPlatform.Fakes.dll"                   = "D1AAC5F3F011"
    }

    $sourceRoot = Join-Path $env:TP_PACKAGES_DIR "microsoft.testplatform\17.3.0\tools\net451\Common7\IDE\Extensions\TestPlatform"
    if (-not (Test-Path $sourceRoot)) {
        throw ("Restore-FakesPayload: the Fakes payload source is not at '$sourceRoot'. It comes from " +
               "the PackageDownload of Microsoft.TestPlatform 17.3.0 in src\package\external\external.csproj, " +
               "so Restore-Package has to run before this.")
    }

    $copied = 0
    foreach ($rel in $map.Keys) {
        $src = Join-Path $sourceRoot $map[$rel]
        $dst = Join-Path $destRoot $rel

        if (-not (Test-Path $src)) {
            throw "Restore-FakesPayload: '$rel' expected at '$src' in Microsoft.TestPlatform 17.3.0 and it is not there."
        }

        $actual = (Get-FileHash $src -Algorithm SHA256).Hash.Substring(0, 12)
        if ($actual -ne $expected[$rel]) {
            throw ("Restore-FakesPayload: '$rel' in Microsoft.TestPlatform 17.3.0 hashes $actual, expected " +
                   "$($expected[$rel]). The payload is not the one that shipped; refusing to stage it.")
        }

        New-Item (Split-Path $dst -Parent) -ItemType Directory -Force | Out-Null
        Copy-Item $src $dst -Force
        $copied++
    }

    # Assert the staged SET, not the number copied. The loop above could complete while an earlier
    # run left something extra behind that nuget would then pack.
    $staged = @(Get-ChildItem $destRoot -Recurse -File | ForEach-Object { $_.FullName.Substring($destRoot.Length + 1) } | Sort-Object)
    $wanted = @($map.Keys | Sort-Object)
    $missing = @($wanted | Where-Object { $staged -notcontains $_ })
    $extra = @($staged | Where-Object { $wanted -notcontains $_ })
    if ($missing.Count -gt 0 -or $extra.Count -gt 0) {
        throw ("Restore-FakesPayload: staged set does not match. Missing: $($missing -join ', '). " +
               "Unexpected: $($extra -join ', ').")
    }

    Write-Log "Restore-FakesPayload: $copied of $($map.Count) files staged into $destRoot, all hash-verified against the shipped 17.3.0 package."
    Write-Log "Restore-FakesPayload: Complete. {$(Get-ElapsedTime($timer))}"
}

function Copy-Bulk {
    param (
        [string]$root,
        [hashtable]$files
    )

    $files.GetEnumerator() | ForEach-Object {
        $from = Join-Path $root $_.Name
        $to = $_.Value

        New-Item -ItemType directory -Path "$to\" -Force | Out-Null
        Copy-Item "$from\*" $to -Force -Recurse
    }
}
function Start-Timer
{
    return [System.Diagnostics.Stopwatch]::StartNew()
}

function Get-ElapsedTime([System.Diagnostics.Stopwatch] $timer)
{
    $timer.Stop()
    return $timer.Elapsed
}

function Set-ScriptFailedOnError
{
    param ($Command, $Arguments, $ExitCode)
    if (0 -eq $ExitCode) {
        return
    }

    if ($FailFast) {
        Write-Error "Build failed. Stopping as fail fast is set.`nFailed command: $Command $Arguments`nExit code: $ExitCode"
    }

    $script:ScriptFailedCommands += "$Command $Arguments"
    $Script:ScriptFailed = $true
}

function PrintAndExit-OnError([System.String] $output)
{
    if ($? -eq $false){
        Write-Error $output
        Exit 1
    }
}

function Invoke-Exe {
    param (
        [Parameter(Mandatory)]
        [string] $Command,
        [string] $Arguments,
        [int[]] $IgnoreExitCode,
        [switch] $CaptureOutput
    )
    Write-Verbose "Invoking: > $Command $Arguments"
    Write-Verbose "          > Ignored exit-codes: 0, $($IgnoreExitCode -join ', ')"

    $workingDirectory = [System.IO.Path]::GetDirectoryName($Command)
    $process = Start-InlineProcess -Path $Command -Arguments $Arguments -WorkingDirectory $workingDirectory -SuppressOutput:$CaptureOutput.IsPresent
    $exitCode = $process.ExitCode

    Write-Verbose "Done. Exit code: $exitCode"

    if ($exitCode -ne 0 -and ($IgnoreExitCode -notcontains $exitCode)) {
        if($CaptureOutput)
        {
            $process.StdErr
        }
        Set-ScriptFailedOnError -Command $Command -Arguments $Arguments -ExitCode $exitCode
    }

    if($CaptureOutput)
    {
        $process.StdOut
    }
}

Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Collections.Generic;

public class ProcessOutputter
{
    private readonly ConsoleColor _color;
    private readonly ConsoleColor _warningColor;
    private readonly ConsoleColor _errorColor;
    private readonly List<string> _output;
    private int nullCount = 0;

    public ProcessOutputter(ConsoleColor color, ConsoleColor warningColor, ConsoleColor errorColor, bool suppressOutput = false)
    {
        _color = color;
        _warningColor = warningColor;
        _errorColor = errorColor;
        _output = new List<string>();

        OutputHandler = (s, e) =>
        {
            AppendLine(e.Data);

            if (suppressOutput || e.Data == null)
            {
                return;
            }

            // These handlers can run at the same time,
            // without lock they sometimes grab the color the other
            // one set.
            lock (Console.Out)
            {
                var fg = Console.ForegroundColor;
                try
                {
                    var lines = e.Data.Split('\n');
                    foreach (var line in lines)
                    {
                        // one extra space before the word, to avoid highlighting
                        // warnaserror and similar parameters that are not actual errors
                        //
                        // The comparison is not done using the insensitive overload because that
                        // is too new for PowerShell 5 compiler
                        var lineToLower = line.ToLowerInvariant();
                        Console.ForegroundColor = lineToLower.Contains(" warning")
                            ? _warningColor
                            : lineToLower.Contains(" error")
                                ? _errorColor
                                : _color;

                        Console.WriteLine(line);
                    }
                }
                finally
                {
                    Console.ForegroundColor = fg;
                }
            }
        };
    }

    public ProcessOutputter()
    {
        _output = new List<string>();
        OutputHandler = (s, e) => AppendLine(e.Data);
    }

    public System.Diagnostics.DataReceivedEventHandler OutputHandler { get; private set; }
    public IEnumerable<string> Output { get { return _output; } }

    private void AppendLine(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            nullCount++;
            return;
        }

        while (nullCount > 0)
        {
            --nullCount;
            _output.Add(string.Empty);
        }

        _output.Add(line);
    }
}
'@

function Start-InlineProcess {
    param (
        [string]
        $Path,

        [string]
        $WorkingDirectory,

        [string]
        $Arguments,

        [switch]
        $Elevate,

        [switch]
        $SuppressOutput
    )

    $processInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $processInfo.FileName = $Path
    $processInfo.Arguments = $Arguments
    $processInfo.WorkingDirectory = $WorkingDirectory

    $processInfo.RedirectStandardError = $true
    $processInfo.RedirectStandardOutput = $true
    $processInfo.UseShellExecute = $false

    if ($Elevate) {
        $processInfo.Verb = "runas"
    }

    $outputHandler = [ProcessOutputter]::new("White", "Yellow", "Red", $SuppressOutput.IsPresent)
    $errorHandler = [ProcessOutputter]::new("White", "Yellow", "Red", $SuppressOutput.IsPresent)
    $outputDataReceived = $outputHandler.OutputHandler
    $errorDataReceivedEvent = $errorHandler.OutputHandler

    $process = New-Object System.Diagnostics.Process
    $process.EnableRaisingEvents = $true
    $process.add_OutputDataReceived($outputDataReceived)
    $process.add_ErrorDataReceived($errorDataReceivedEvent)

    try {
        $process.StartInfo = $processInfo
        if (-not $process.Start()) {
            return $null
        }
        $process.BeginOutputReadLine()
        $process.BeginErrorReadLine()
        $process.WaitForExit()

        return @{
            ExitCode = $process.ExitCode
            StdOut = $outputHandler.Output
            StdErr = $errorHandler.Output
        }
    }
    finally {
        $process.remove_OutputDataReceived($outputDataReceived)
        $process.remove_ErrorDataReceived($errorDataReceived)
        $process.Dispose()
    }
}
