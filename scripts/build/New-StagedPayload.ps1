<#
.SYNOPSIS
Builds the staged third-party payload directory from the shipped 15.9 artifacts.

THIS SCRIPT RECURSIVELY DELETES -DestinationRoot BEFORE WRITING TO IT. The parameter is
mandatory and caller-supplied, and "New-" reads as purely creative, so this is stated here
rather than only beside the code. It now REFUSES a drive root, a directory containing .git,
and any path that is or contains this repository - but those are backstops, not permission:
pass a directory that exists to be rebuilt.

.DESCRIPTION
scripts\build.ps1 reads third-party payload out of $env:TP_PACKAGES_DIR, which it sets to
<repo>\packages - the folder its own nuget restore filled. Under Arcade that restore does not
happen, and restoring the packages by identity is the wrong answer regardless:

    shipped baseline   x64/msdia140.dll                : 14.20.27106.0
    cache  microsoft.internal.dia 14.0.0  msdia140.dll : 14.26.28619.0

Same package identity, different binary. Four of the nine payload packages have no 15.9-era version
available at all, and of the five that do, one is provably the wrong bytes and the other four are
unverified. Present at the right version is not evidence, so all nine are carried from the shipped
artifacts by one method rather than split by availability.

This script extracts them into a directory laid out the way build.ps1 expects, so the payload sites
work unchanged once TP_PACKAGES_DIR points here.

The shipped baseline is a SET of artifacts, not one file. Measuring against the VSIX alone reported
78 of 87 Intellitrace files missing; against the VSIX and the shipped nupkg together the real figure
is 6, all of them .pdb, plus 2 IntelliSense .XML. Those 8 never shipped, so their absence is a fact
about what shipped rather than a gap in the source - which is why -RequireComplete does not treat
them as failures.

.NOTES
Does NOT set NUGET_PACKAGES. build.ps1:79 derives NUGET_PACKAGES from TP_PACKAGES_DIR, which
repoints NuGet at the repo and creates the <repo>\packages folder that produced 45 false findings in
the shared delivery guard on another line. That derivation must not travel with this fix.
#>
[CmdletBinding()]
param(
    # The shipped artifacts to extract from. All of them together are "the baseline".
    [Parameter(Mandatory = $true)] [string[]] $BaselineArtifact,

    [Parameter(Mandatory = $true)] [string] $DestinationRoot,

    # NuGet global packages folder, used only for the measured exceptions described in $layout -
    # entries whose package has been proven byte-identical to a shipped file.
    [string] $PackageCache = $env:NUGET_PACKAGES,

    # Fail if a declared payload file is not found in any baseline artifact.
    [switch] $RequireComplete
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Where each payload site reads from, relative to TP_PACKAGES_DIR, and which shipped subtree supplies
# it. Package paths are taken verbatim from scripts\build.ps1 so a change there shows up here as an
# empty directory rather than as a silently wrong shape.
#
# SourcePrefix matters as much as the file set. Every site copies with Copy-Item -Recurse, so the
# structure BELOW the prefix is part of the payload: ComComponents ships as x64\ and x86\
# subdirectories holding same-named files of different sizes. Matching on file name alone flattens
# those into one name and silently keeps whichever was extracted last - a staged tree that looks
# populated and has the wrong architecture in it.
$layout = @(
    @{ Package = 'Microsoft.Internal.Dia\14.0.0\contentFiles\any\any\ComComponents'
       SourcePrefix = 'Extensions/TestImpact/ComComponents/' }

    # msdia140typelib_clr0200.dll ships at the VSIX root and in every shipped package, and
    # Create-VsixPackage places it there by copying ComComponents\* into the package root - so it
    # belongs in the staged ComComponents directory even though it is not under ComComponents in
    # the shipped VSIX.
    #
    # It is absent from the restorable Microsoft.Internal.Dia 14.0.0 entirely: that package's
    # contentFiles\any\any holds only a ComComponents directory, with no typelib anywhere. So the
    # restorable package differs from the shipped one in its FILE LIST, not merely in bytes - a
    # second and worse form of the same defect that gives 14.26.28619.0 where 14.20.27106.0 shipped.
    #
    # Found by comparing produced package contents against the shipped set path by path. A
    # directory-list gate could not have found it, because the file is not in any directory the
    # gate knew to look at.
    @{ Package = 'Microsoft.Internal.Dia\14.0.0\contentFiles\any\any\ComComponents'
       SourcePrefix = ''; File = 'msdia140typelib_clr0200.dll'; PathFilter = '^msdia140typelib_clr0200\.dll$' }

    # Publish-Package copies this package's tools\ directory to artifacts\<config>\Intellitrace
    # verbatim, and Microsoft.TestPlatform.nuspec then reads
    # Intellitrace\Common7\IDE\CommonExtensions\Microsoft\IntelliTrace\...,
    # Intellitrace\Common7\IDE\PrivateAssemblies\... and
    # Intellitrace\Team Tools\Dynamic Code Coverage Tools\... So the structure below tools\ is part
    # of the payload and the prefix that may be stripped is tools/net451/ alone.
    #
    # An earlier version stripped tools/net451/Common7/IDE/CommonExtensions/Microsoft/IntelliTrace/
    # whole. That flattened those 70 files to the root of tools\ and staged neither
    # PrivateAssemblies nor Team Tools. Publish-Package copied 72 files and reported success; the
    # wrong shape only surfaced when Microsoft.TestPlatform.nuspec was finally packed and could not
    # find the directory. A copy that lands in the wrong place is not an error to Copy-Item.
    #
    # tools/net451/ also holds the product this repo builds, which is why the three subtrees are
    # named rather than taking the prefix whole.
    @{ Package = 'Microsoft.Internal.Intellitrace\15.9.0-preview-20180924-02\tools'
       SourcePrefix = 'tools/net451/'
       PathFilter = '^tools/net451/(Common7/IDE/CommonExtensions/Microsoft/IntelliTrace/|Common7/IDE/PrivateAssemblies/|Team%20Tools/Dynamic%20Code%20Coverage%20Tools/)' }

    @{ Package = 'newtonsoft.json\9.0.1\lib\net45'
       SourcePrefix = ''; File = 'Newtonsoft.Json.dll'; PathFilter = '/net451/' }

    # The only Newtonsoft copy in the VSIX and the Microsoft.TestPlatform package is the net45 one.
    # The netcore payload ships in Microsoft.TestPlatform.Portable and Microsoft.TestPlatform.CLI,
    # which are part of the shipped set too - "the baseline" is every shipped artifact, not the
    # nearest one to hand.
    #
    # Sourced from the restored package, and verified DIRECTLY against its own shipped counterpart:
    # tools/netcoreapp2.0/Newtonsoft.Json.dll in Microsoft.TestPlatform.Portable 15.9.2 is
    # byte-identical to the netstandard1.0 assembly in newtonsoft.json 9.0.1 - sha256
    # 5D96EE51B2AFF592, 468,480 bytes on both sides.
    #
    # An earlier version of this verified a DIFFERENT file - the net45 assembly - and inferred that
    # its sibling must therefore be correct. That extends a file measurement to a package, which is
    # exactly the reasoning the DIA finding disproves: a measurement licenses the file that was
    # measured, not the package it came from. The comparison below licenses the file it stages.
    #
    # This is the opposite result to Microsoft.Internal.Dia, whose restorable package carries
    # 14.26.28619.0 against the shipped 14.20.27106.0. Neither outcome may be assumed for a third
    # package - measure each one against its own shipped counterpart.
    @{ Package = 'newtonsoft.json\9.0.1\lib\netstandard1.0'
       FromPackageCache = 'newtonsoft.json\9.0.1\lib\netstandard1.0\Newtonsoft.Json.dll'
       VerifyAgainstShipped = @{ CachePath = 'newtonsoft.json\9.0.1\lib\netstandard1.0\Newtonsoft.Json.dll'; ShippedPath = 'tools/netcoreapp2.0/Newtonsoft.Json.dll' } }

    @{ Package = 'Microsoft.VisualStudio.Telemetry\15.6.815-master284DF69C\lib\net45'
       SourcePrefix = ''; File = 'Microsoft.VisualStudio.Telemetry.dll' }

    @{ Package = 'Microsoft.VisualStudio.Utilities.Internal\14.0.74-masterCEEA65A3\lib\net45'
       SourcePrefix = ''; File = 'Microsoft.VisualStudio.Utilities.Internal.dll' }

    # --- Legacy VS test tooling: four packages that exist on no reachable feed ------------------
    #
    # Microsoft.Internal.TestPlatform.Extensions, Microsoft.VisualStudio.QualityTools,
    # Microsoft.VisualStudio.QualityTools.DataCollectors and Microsoft.VisualStudio.CUIT at
    # 15.9.1-preview-2124990 were searched across vssdk, vssdk-archived, vs-impl, vs-impl-archived,
    # dotnet-eng, test-tools, dotnet-tools and dotnet-public and are on none of them. Restoring them
    # by identity resolves 16.8/16.9 packages into a 2018 build, which is a substitution rather than
    # a carry.
    #
    # Create-VsixPackage copies contentFiles\any\any\* from each of the four into net451\win7-x64,
    # so they supply legacy VS test tooling this repo never compiles - QualityTools, CUIT plugins,
    # QTAgent, the video recorder, TMI. Under the partition rule those are Carried and may come from
    # any authentic shipped artifact. Every path below is taken from the shipped VSIX.
    #
    # HOW THE SET WAS DERIVED, AND WHAT IS NOT KNOWN
    #
    # The set is what the shipped VSIX contains and a full build followed by Publish and the
    # payload half of Create-VsixPackage does not produce - measured, not guessed. 145 paths.
    # Four are excluded and each exclusion is named rather than dropped silently:
    #
    #   package/services/digital-signature/_rels/origin.psdor.rels   VSIX signature part, not payload
    #   package/services/digital-signature/origin.psdor   VSIX signature part, not payload
    #   package/services/digital-signature/xml-signature/9t5s3ds8mi9g9fu3uw2yi3xdh.psdsxs   VSIX signature part, not payload
    #   Extensions/Microsoft.VisualStudio.TraceDataCollector.dll
    #     ^ NOT excluded. It was, on the strength of the name appearing in artifacts\bin, and that
    #       was wrong. The name ships at four different sizes and they are different assemblies:
    #
    #         our build,       artifacts\bin netstandard2.0                40,960
    #         Microsoft.CodeCoverage 15.9.2  build/netstandard1.0/         57,088
    #         Microsoft.TestPlatform 15.9.2  .../Extensions/              166,960
    #         shipped VSIX                   Extensions/                  167,168   (+208, re-signed)
    #
    #       The Extensions\ copy is four times our build output, so nothing this repo compiles
    #       produces it and it is Carried like the rest of this set. Our netstandard2.0 output is a
    #       different delivery: Copy-CodeCoverage-Package-Artifacts places it in
    #       artifacts\<config>\Microsoft.CodeCoverage for the Microsoft.CodeCoverage package.
    #
    #       Recorded rather than quietly fixed, because the mistake was matching on FILE NAME - the
    #       rule this same file states two entries above, broken while enforcing it.
    #
    # That leaves 142. Of those, 119 are attributed to a single package by matching the FULL
    # RELATIVE PATH against the file list of the same package at 17.10.0-preview-2-34602-162 from
    # the machine cache. The newer package is used ONLY as a manifest - which path belongs to
    # which package - and never as a source of bytes; the bytes come from the shipped VSIX in
    # every case. The remaining 23 match no 17.10 path and their owning package is NOT RECOVERABLE:
    # all four copy into the same destination, so the split leaves no trace in any shipped
    # artifact. They are listed under Microsoft.Internal.TestPlatform.Extensions and marked, so the
    # attribution is stated as partial rather than implied by a tidy-looking split.
    #
    # The staged result is identical either way, because all four sites copy to net451\win7-x64.
    # The attribution matters for the provenance record, not for the output.

    @{ Package = 'Microsoft.Internal.TestPlatform.Extensions\15.9.1-preview-2124990\contentFiles\any\any'
       SourcePrefix = ''
       Paths = @(
           'Extensions/Cpp/dbghelp.dll'
           'Extensions/Cpp/Microsoft.VisualStudio.TestTools.CppUnitTestFramework.Discoverer.dll'
           'Extensions/Cpp/Microsoft.VisualStudio.TestTools.CppUnitTestFramework.dll'
           'Extensions/Cpp/Microsoft.VisualStudio.TestTools.CppUnitTestFramework.Executor.dll'
           'Extensions/Cpp/Microsoft.VisualStudio.TestTools.CppUnitTestFramework.Executor.dll.manifest'
           'Extensions/Cpp/Microsoft.VisualStudio.TestTools.CppUnitTestFramework.Executor.Resources.dll'
           'Extensions/Cpp/Microsoft.VisualStudio.TestTools.CppUnitTestFramework.Executor.x64.dll'
           'Extensions/Cpp/Microsoft.VisualStudio.TestTools.CppUnitTestFramework.Executor.x64.dll.manifest'
           'Extensions/Cpp/Microsoft.VisualStudio.TestTools.CppUnitTestFramework.Resources.dll'
           'Extensions/Cpp/Microsoft.VisualStudio.TestTools.CppUnitTestFramework.TestEngine.dll'
           'Extensions/Cpp/Microsoft.VisualStudio.TestTools.CppUnitTestFramework.x64.dll'
           'Extensions/Cpp/x64/dbghelp.dll'
           'Extensions/Microsoft.VisualStudio.TestPlatform.Extensions.CodedWebTestAdapter.dll'
           'Extensions/Microsoft.VisualStudio.TestPlatform.Extensions.TmiAdapter.dll'
           'Extensions/Microsoft.VisualStudio.TestPlatform.Extensions.VSTestIntegration.dll'
           'Extensions/Microsoft.VisualStudio.TestPlatform.Extensions.WebTestAdapter.dll'
           'Extensions/Microsoft.VisualStudio.TestTools.CppUnitTestFramework.ComInterfaces.dll'
           'Extensions/Microsoft.VisualStudio.TestTools.CppUnitTestFramework.CppUnitTestExtension.dll'
           'Extensions/Microsoft.VisualStudio.TestTools.DataCollection.MediaRecorder.Model.dll'
           'Extensions/Microsoft.VisualStudio.TestTools.DataCollection.VideoRecorderCollector.dll'
           'Extensions/VideoRecorder/Microsoft.VisualStudio.QualityTools.VideoRecorderEngine.dll'
           'Extensions/VideoRecorder/Microsoft.VisualStudio.QualityTools.VideoRecorderEngine.pdb'
           'Extensions/VideoRecorder/Microsoft.VisualStudio.TestTools.DataCollection.MediaRecorder.Model.dll'
           'Extensions/VideoRecorder/Microsoft.VisualStudio.TestTools.DataCollection.MediaRecorder.Model.pdb'
           'Extensions/VideoRecorder/VSTestVideoRecorder.exe'
           'Extensions/VideoRecorder/VSTestVideoRecorder.pdb'
           'Microsoft.DiaSymReader.dll'
           'Microsoft.IntelliTrace.Core.dll'
           'Microsoft.VisualStudio.ArchitectureTools.PEReader.dll'
           'Microsoft.VisualStudio.Diagnostics.Utilities.dll'
           'Microsoft.VisualStudio.Enterprise.AspNetHelper.dll'

           # Below here: present in the shipped VSIX, produced by no build step, and matching no
           # path in any of the four packages at 17.10. Owning package not recoverable.
           'CoreCon/VisualStudioDatastoreConfigurationProvider.dll'
           'CoreCon/VisualStudioDatastoreConfigurationProvider.pdb'
           'Extensions/Microsoft.VisualStudio.Coverage.Interop.dll'
           'Extensions/Microsoft.VisualStudio.TestPlatform.Extensions.GenericTestAdapter.dll'
           'Extensions/Microsoft.VisualStudio.TraceDataCollector.dll'
           'Extensions/Microsoft.VisualStudio.TestPlatform.Extensions.OrderedTestAdapter.dll'
           'Extensions/V1/Microsoft.VisualStudio.QualityTools.VideoRecorderEngine.dll'
           'Extensions/V1/Microsoft.VisualStudio.TestTools.DataCollection.MediaRecorder.Model.dll'
           'Extensions/V1/VSTestVideoRecorder.exe'
           'License.rtf'
           'Microsoft.VisualStudio.Coverage.Analysis.dll'
           'Microsoft.VisualStudio.Coverage.Interop.dll'
           'Microsoft.VisualStudio.OLE.Interop.dll'
           'Microsoft.VisualStudio.QualityTools.Tips.GenericTest.Adapter.dll'
           'Microsoft.VisualStudio.QualityTools.Tips.GenericTest.ObjectModel.dll'
           'Microsoft.VisualStudio.QualityTools.Tips.GenericTest.Tip.dll'
           'Microsoft.VisualStudio.QualityTools.Tips.OrderedTest.Adapter.dll'
           'Microsoft.VisualStudio.QualityTools.Tips.OrderedTest.ObjectModel.dll'
           'Microsoft.VisualStudio.QualityTools.Tips.OrderedTest.Tip.dll'
           'Microsoft.VisualStudio.TestPlatform.Fakes.dll'
           'SummaryResult.xsd'
           'testplatform.config'
           'VsWebSite.Interop.dll'
       ) }

    @{ Package = 'Microsoft.VisualStudio.QualityTools\15.9.1-preview-2124990\contentFiles\any\any'
       SourcePrefix = ''
       Paths = @(
           'LegacyTypes.testtype'
           'ManualTests.testtype'
           'Microsoft.VisualStudio.QualityTools.AgentObject.dll'
           'Microsoft.VisualStudio.QualityTools.AgentProcessManager.dll'
           'Microsoft.VisualStudio.QualityTools.Common.dll'
           'Microsoft.VisualStudio.QualityTools.ControllerObject.dll'
           'Microsoft.VisualStudio.QualityTools.ExecutionCommon.dll'
           'Microsoft.VisualStudio.QualityTools.HostAdapters.ASPNETAdapter.dll'
           'Microsoft.VisualStudio.QualityTools.HostAdapters.ASPNETAgent.dll'
           'Microsoft.VisualStudio.QualityTools.LoadTest.dll'
           'Microsoft.VisualStudio.QualityTools.LoadTestFramework.dll'
           'Microsoft.VisualStudio.QualityTools.Resource.dll'
           'Microsoft.VisualStudio.QualityTools.Tips.ManualTest.Adapter.dll'
           'Microsoft.VisualStudio.QualityTools.Tips.ManualTest.ObjectModel.dll'
           'Microsoft.VisualStudio.QualityTools.Tips.ManualTest.Tip.dll'
           'Microsoft.VisualStudio.QualityTools.Tips.UnitTest.Adapter.dll'
           'Microsoft.VisualStudio.QualityTools.Tips.UnitTest.AssemblyResolver.dll'
           'Microsoft.VisualStudio.QualityTools.Tips.UnitTest.ObjectModel.dll'
           'Microsoft.VisualStudio.QualityTools.Tips.UnitTest.Tip.dll'
           'Microsoft.VisualStudio.QualityTools.Tips.WebLoadTest.Tip.dll'
           'Microsoft.VisualStudio.QualityTools.TMI.dll'
           'Microsoft.VisualStudio.QualityTools.UnitTestFramework.dll'
           'Microsoft.VisualStudio.QualityTools.WebTestFramework.dll'
           'Microsoft.VisualStudio.TestTools.UITest.Extension.IE.Communication.sxs.manifest'
           'Microsoft.VisualStudio.TestTools.UITest.Playback.Engine.sxs.manifest'
           'QTAgent_35.exe'
           'QTAgent_35.exe.Config'
           'QTAgent_40.exe'
           'QTAgent_40.exe.Config'
           'QTAgent.exe'
           'QTAgent.exe.Config'
           'QTAgent32_35.exe'
           'QTAgent32_35.exe.Config'
           'QTAgent32_40.exe'
           'QTAgent32_40.exe.Config'
           'QTAgent32.exe'
           'QTAgent32.exe.Config'
           'QTDCAgent.exe'
           'QTDCAgent.exe.Config'
           'QTDCAgent32.exe'
           'QTDCAgent32.exe.Config'
           'Webloadtest.testtype'
       ) }

    @{ Package = 'Microsoft.VisualStudio.QualityTools.DataCollectors\15.9.1-preview-2124990\contentFiles\any\any'
       SourcePrefix = ''
       Paths = @(
           'Extensions/V1/Microsoft.VisualStudio.QualityTools.Plugins.CodeCoverage.dll'
           'Extensions/V1/Microsoft.VisualStudio.QualityTools.Plugins.CodeCoverage.dll.config'
           'Extensions/V1/Microsoft.VisualStudio.TestTools.DataCollection.EventLog.dll'
           'Extensions/V1/Microsoft.VisualStudio.TestTools.DataCollection.EventLog.dll.config'
           'Extensions/V1/Microsoft.VisualStudio.TestTools.DataCollection.SystemInfo.dll'
           'Extensions/V1/Microsoft.VisualStudio.TestTools.DataCollection.VideoRecorder.dll'
           'Extensions/V1/Microsoft.VisualStudio.TestTools.DataCollection.VideoRecorder.dll.config'
           'Extensions/V1/Microsoft.VisualStudio.TraceCollector.dll'
           'Extensions/V1/Microsoft.VisualStudio.TraceCollector.dll.config'
           'Extensions/V1/VSTestVideoRecorder.exe.config'
           'Extensions/V1/x64/Microsoft.VisualStudio.Coverage.Monitor.dll'
           'Extensions/V1/x64/Microsoft.VisualStudio.Coverage.Monitor.pdb'
           'Extensions/V1/x86/Microsoft.VisualStudio.Coverage.Monitor.dll'
           'Extensions/V1/x86/Microsoft.VisualStudio.Coverage.Monitor.pdb'
           'Microsoft.VisualStudio.QualityTools.DataCollectionAgentObject.dll'
       ) }

    @{ Package = 'Microsoft.VisualStudio.CUIT\15.9.1-preview-2124990\contentFiles\any\any'
       SourcePrefix = ''
       Paths = @(
           'CUITPlugins/IEPropertyConfiguration.xml'
           'CUITPlugins/IEPropertyConfiguration.xsd'
           'CUITPlugins/Microsoft.VisualStudio.TestTools.UITest.Extension.CrossBrowser.dll'
           'CUITPlugins/Microsoft.VisualStudio.TestTools.UITest.Extension.IE.Communication.dll'
           'CUITPlugins/Microsoft.VisualStudio.TestTools.UITest.Extension.IE.Communication.Interop.dll'
           'CUITPlugins/Microsoft.VisualStudio.TestTools.UITest.Extension.IE.dll'
           'CUITPlugins/Microsoft.VisualStudio.TestTools.UITest.Extension.IE.EventHelper.dll'
           'CUITPlugins/Microsoft.VisualStudio.TestTools.UITest.Extension.IE64.Communication.dll'
           'CUITPlugins/Microsoft.VisualStudio.TestTools.UITest.Extension.IE64.EventHelper.dll'
           'CUITPlugins/Microsoft.VisualStudio.TestTools.UITest.Extension.Uia.dll'
           'CUITPlugins/Microsoft.VisualStudio.TestTools.UITest.Extension.UiaWidget.dll'
           'CUITPlugins/Microsoft.VisualStudio.TestTools.UITest.Extension.UiaWidget.UIAHtmlElementUtilities.dll'
           'Interop.UIAutomationClient.dll'
           'Microsoft.ShDocVw.dll'
           'Microsoft.VisualStudio.Diagnostics.Measurement.dll'
           'Microsoft.VisualStudio.QualityTools.CodedUITestFramework.dll'
           'Microsoft.VisualStudio.QualityTools.Sqm.dll'
           'Microsoft.VisualStudio.TestTools.UITest.Common.dll'
           'Microsoft.VisualStudio.TestTools.UITest.Extension.dll'
           'Microsoft.VisualStudio.TestTools.UITest.Extension.IE.Communication.dll'
           'Microsoft.VisualStudio.TestTools.UITest.Extension.IE.Communication.Interop.dll'
           'Microsoft.VisualStudio.TestTools.UITest.Extension.Msaa.dll'
           'Microsoft.VisualStudio.TestTools.UITest.ExtensionUtilities.dll'
           'Microsoft.VisualStudio.TestTools.UITest.Framework.dll'
           'Microsoft.VisualStudio.TestTools.UITest.Logging.dll'
           'Microsoft.VisualStudio.TestTools.UITest.Playback.dll'
           'Microsoft.VisualStudio.TestTools.UITest.Playback.Engine.dll'
           'Microsoft.VisualStudio.TestTools.UITest.Synchronization.dll'
           'Microsoft.VisualStudio.TestTools.UITest.WindowsStoreUtility.dll'
           'Microsoft.VisualStudio.TestTools.UITesting.dll'
           'UiaComWrapper.dll'
       ) }

)

Write-Host "Staging payload from $($BaselineArtifact.Count) baseline artifact(s)"

# Every payload package build.ps1 reads must have a layout entry above.
#
# The denominator is scripts\build.ps1 itself, not a list maintained here. A hand-written list
# covers what its author knew about; the read sites are the definition of what must be staged. This
# file's own header declared nine payload packages while $layout implemented five for a week,
# because the four it omitted are read only by Create-VsixPackage and nobody had run that step. The
# claim and the behaviour were never compared, so nothing was there to notice.
#
# Copy-Item is why this has to be checked here rather than left to fail at the copy. A missing
# source directory throws, but an EMPTY one does not: Copy-Item on a wildcard that matches nothing
# is not an error. So an entry that stages zero files copies nothing, silently, and the defect
# surfaces much later as missing paths in a package.
#
# LIMIT, stated because a limit that lives in a conversation is a limit nobody will know about:
# this compares PACKAGE IDS, not full paths. It answers "is any package never staged", which is the
# failure that occurred. It does NOT catch a layout entry that stages the right package at the
# wrong depth - that was a real defect in the Intellitrace entry and it is caught instead by the
# declared-path check in the staging loop, which throws when a named path is in no baseline
# artifact.
$buildScript = Join-Path $PSScriptRoot '..\build.ps1'
if (-not (Test-Path $buildScript)) { throw "cannot verify staging coverage: $buildScript not found" }

# Tooling, not payload. These resolve from NUGET_PACKAGES and must never be staged into a tree
# defined as "extracted from the shipped artifacts" - nuget.exe and vswhere do not ship, so putting
# them here would corrupt the definition of this tree rather than fix anything.
$toolingPackages = @('vswhere', 'Nuget.CommandLine')

$sites = [regex]::Matches(
        (Get-Content $buildScript -Raw),
        'TP_PACKAGES_DIR\s+(?:-ChildPath\s+)?"([^"]+)"') |
    ForEach-Object { ($_.Groups[1].Value -split '\\')[0] } |
    Sort-Object -Unique |
    Where-Object { $toolingPackages -notcontains $_ }

$stagedPackages = $layout | ForEach-Object { ($_.Package -split '\\')[0] } | Sort-Object -Unique
$uncovered = @($sites | Where-Object { $stagedPackages -notcontains $_ })

if ($uncovered) {
    throw ("build.ps1 reads $($uncovered.Count) payload package(s) that this layout does not stage, " +
           "so their copies would silently produce nothing: " + ($uncovered -join ', '))
}
Write-Host "  payload coverage: $($sites.Count) package(s) read by build.ps1, all staged"

# Index every entry in every baseline artifact once. Later artifacts do not overwrite earlier ones:
# the same file ships in more than one artifact and the copies are identical, so first wins and the
# provenance recorded is the artifact it was actually taken from.
$index = @{}
foreach ($artifact in $BaselineArtifact) {
    if (-not (Test-Path $artifact)) { throw "baseline artifact not found: $artifact" }
    $zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path $artifact).Path)
    try {
        foreach ($entry in $zip.Entries) {
            if (-not $entry.Name) { continue }
            $key = $entry.FullName
            if (-not $index.ContainsKey($key)) {
                $index[$key] = [pscustomobject]@{
                    Artifact = (Split-Path $artifact -Leaf)
                    FullName = $entry.FullName
                    Name     = $entry.Name
                    Length   = $entry.Length
                }
            }
        }
    } finally { $zip.Dispose() }
}
Write-Host "  indexed $($index.Count) distinct entries"

if ($index.Count -eq 0) { throw "baseline index is empty - refusing to stage from nothing" }

# REFUSE TO RECURSIVELY DELETE ANYTHING THAT IS NOT PLAINLY A STAGING DIRECTORY.
#
# -DestinationRoot is a MANDATORY CALLER-SUPPLIED path and the next line deletes it with
# -Recurse -Force. Passed a repository root, a worktree, or a drive root, this destroys work that
# is not ours - and the name New-StagedPayload reads as purely creative, so nothing warns the
# caller before they find out.
#
# Found by applying 16.11's population rule to my own deletion audit: that audit filtered
# scripts\build\*.ps1 to names matching Assert-|Measure-|Test-, which covered 6 of 8 files and
# excluded this one. I then reported "none takes a caller-supplied path". The answer was right
# about the six I looked at and wrong about the population, and only the second is a property of
# the check.
#
# The checks below are structural and deliberately conservative. They cost nothing on a real
# staging directory and refuse the cases that cannot be undone.
$destFull = [IO.Path]::GetFullPath($DestinationRoot)
if ($destFull -eq [IO.Path]::GetPathRoot($destFull)) {
    throw "REFUSING to delete a drive root: '$destFull'. -DestinationRoot must be a staging directory."
}
if (Test-Path (Join-Path $destFull '.git')) {
    throw "REFUSING to delete '$destFull': it contains a .git entry, so it is a repository or worktree root, not a staging directory."
}
$srcRoot = (& git rev-parse --show-toplevel 2>$null)
if ($srcRoot) {
    $srcFull = [IO.Path]::GetFullPath(($srcRoot -replace '/', [IO.Path]::DirectorySeparatorChar))
    if ($destFull -eq $srcFull -or $srcFull.StartsWith($destFull.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "REFUSING to delete '$destFull': it is, or contains, this repository at '$srcFull'."
    }
}

if (Test-Path $DestinationRoot) { Remove-Item $DestinationRoot -Recurse -Force }
New-Item $DestinationRoot -ItemType Directory | Out-Null

$staged = @()
foreach ($item in $layout) {
    $targetDir = Join-Path $DestinationRoot $item.Package
    New-Item $targetDir -ItemType Directory -Force | Out-Null

    if ($item.FromPackageCache) {
        # Measured exception: sourced from the restored package, but only after re-proving on this
        # run that the package is the one that shipped. The verification compares a DIFFERENT file
        # from the same package - one that does ship - against the baseline. If that check is
        # skipped the exception silently degrades into restoring by identity, which is the defect
        # this whole script exists to avoid.
        if (-not $PackageCache) { throw "$($item.Package): needs -PackageCache, or NUGET_PACKAGES set" }

        $v = $item.VerifyAgainstShipped
        $proof = Join-Path $PackageCache $v.CachePath
        if (-not (Test-Path $proof)) { throw "$($item.Package): verification file not in the package cache: $proof" }

        # Match on the full shipped path, not the file name. A name can occur many times across the
        # shipped set at different sizes - Newtonsoft.Json.dll ships at both 526,336 and 468,480 -
        # so a name match can compare against the wrong copy and pass or fail for the wrong reason.
        $shipped = $index.Values | Where-Object { $_.FullName -eq $v.ShippedPath } | Select-Object -First 1
        if (-not $shipped) { throw "$($item.Package): $($v.ShippedPath) not found in the baseline set - cannot verify, refusing to stage" }

        $tmp = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString('N'))
        $srcZip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path ($BaselineArtifact | Where-Object { (Split-Path $_ -Leaf) -eq $shipped.Artifact } | Select-Object -First 1)).Path)
        try { [IO.Compression.ZipFileExtensions]::ExtractToFile($srcZip.GetEntry($shipped.FullName), $tmp, $true) } finally { $srcZip.Dispose() }

        $hShipped = (Get-FileHash $tmp -Algorithm SHA256).Hash
        $hCache = (Get-FileHash $proof -Algorithm SHA256).Hash
        Remove-Item $tmp -Force -ErrorAction SilentlyContinue

        if ($hShipped -ne $hCache) {
            throw "$($item.Package): package is NOT the shipped one - $($v.ShippedName) shipped=$($hShipped.Substring(0,16)) cache=$($hCache.Substring(0,16)). Refusing to source from it."
        }
        Write-Host ("  {0,-72} verified {1}" -f $item.Package, $hCache.Substring(0, 16))

        $file = Join-Path $PackageCache $item.FromPackageCache
        if (-not (Test-Path $file)) { throw "$($item.Package): $file not found" }
        $dest = Join-Path $targetDir (Split-Path $item.FromPackageCache -Leaf)
        Copy-Item $file $dest -Force
        $staged += [pscustomobject]@{
            Package = $item.Package; RelativePath = (Split-Path $item.FromPackageCache -Leaf); File = (Split-Path $item.FromPackageCache -Leaf)
            Source = "package cache (verified against shipped $($v.ShippedName))"; SourcePath = $item.FromPackageCache; Bytes = (Get-Item $file).Length
        }
        continue
    }

    if ($item.Paths) {
        # Explicit manifest: the carried set is named file by file rather than taken as a subtree,
        # because the subtree it sits in also holds output this repo builds. Taking the whole tree
        # would carry our own product and classify a Rebuilt file as Carried, which is the failure
        # the three-bucket rule exists to prevent.
        $wanted = @{}
        foreach ($p in $item.Paths) { $wanted[($item.SourcePrefix + $p)] = $p }
        $matched = $index.Values |
            Where-Object { $wanted.ContainsKey($_.FullName) } |
            ForEach-Object {
                $_ | Add-Member -NotePropertyName RelativePath -NotePropertyValue $wanted[$_.FullName] -PassThru -Force
            }

        $absent = @($item.Paths | Where-Object { $p = $_; -not ($matched | Where-Object { $_.RelativePath -eq $p }) })
        if ($absent) {
            throw "$($item.Package): $($absent.Count) declared path(s) are in no baseline artifact, first: $($absent[0])"
        }
    }
    elseif ($item.SourcePrefix) {
        # Prefix-anchored: take the whole subtree and keep its shape. RelativePath is what makes
        # x64\ and x86\ survive as separate directories instead of colliding on file name.
        #
        # PathFilter narrows the subtree. tools/net451/ holds the product we build alongside the
        # carried tooling, so the prefix that gets stripped and the set that gets taken are two
        # different decisions and each is stated separately.
        #
        # Zip entry names are URL-encoded, so "Team Tools" arrives as "Team%20Tools". The decode is
        # applied to RelativePath only: the entry must still be looked up by its encoded name.
        $matched = $index.Values |
            Where-Object {
                $_.FullName.StartsWith($item.SourcePrefix, [StringComparison]::OrdinalIgnoreCase) -and
                (-not $item.PathFilter -or $_.FullName -match $item.PathFilter)
            } |
            ForEach-Object {
                $rel = [Uri]::UnescapeDataString($_.FullName.Substring($item.SourcePrefix.Length))
                $_ | Add-Member -NotePropertyName RelativePath -NotePropertyValue $rel -PassThru -Force
            }
    }
    else {
        # Single named file. Still records a RelativePath so both branches stage the same way.
        $matched = $index.Values |
            Where-Object { $_.Name -eq $item.File -and (-not $item.PathFilter -or $_.FullName -match $item.PathFilter) } |
            Select-Object -First 1 |
            ForEach-Object { $_ | Add-Member -NotePropertyName RelativePath -NotePropertyValue $_.Name -PassThru -Force }
    }

    foreach ($m in $matched) {
        if ($staged | Where-Object { $_.Package -eq $item.Package -and $_.RelativePath -eq $m.RelativePath }) { continue }
        $staged += [pscustomobject]@{
            Package = $item.Package; RelativePath = $m.RelativePath; File = $m.Name
            Source = $m.Artifact; SourcePath = $m.FullName; Bytes = $m.Length
        }
    }
    Write-Host ("  {0,-72} {1} file(s)" -f $item.Package, ($matched | Measure-Object).Count)
}

# Extract, recording provenance per file so a wrong-bytes question later has an answer.
foreach ($artifact in $BaselineArtifact) {
    $leaf = Split-Path $artifact -Leaf
    $want = $staged | Where-Object { $_.Source -eq $leaf }
    if (-not $want) { continue }
    $zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path $artifact).Path)
    try {
        foreach ($w in $want) {
            $entry = $zip.GetEntry($w.SourcePath)
            if (-not $entry) { throw "entry vanished between index and extract: $($w.SourcePath)" }
            $dest = Join-Path (Join-Path $DestinationRoot $w.Package) $w.RelativePath
            $destDir = Split-Path $dest -Parent
            if (-not (Test-Path $destDir)) { New-Item $destDir -ItemType Directory -Force | Out-Null }
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $dest, $true)
        }
    } finally { $zip.Dispose() }
}

$manifest = Join-Path $DestinationRoot 'payload-provenance.csv'
$staged | Select-Object Package, RelativePath, Source, SourcePath, Bytes | Export-Csv $manifest -NoTypeInformation

Write-Host ""
Write-Host "Staged $($staged.Count) file(s) to $DestinationRoot"
Write-Host "Provenance: $manifest"

$empty = $layout | Where-Object { $pkg = $_.Package; -not ($staged | Where-Object { $_.Package -eq $pkg }) }
if ($empty) {
    Write-Host ""
    Write-Host "EMPTY payload directories:" -ForegroundColor Yellow
    $empty | ForEach-Object { Write-Host ("  {0}" -f $_.Package) -ForegroundColor Yellow }
    if ($RequireComplete) { throw "$($empty.Count) declared payload director(ies) staged nothing" }
}

$staged
