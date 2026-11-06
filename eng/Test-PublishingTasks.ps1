# Restores and loads the real Arcade publishing tasks without executing them.
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$restore = $true
$ci = $false
$configuration = 'Release'
$msbuildEngine = 'dotnet'
$disableConfigureToolsetImport = $true
. $PSScriptRoot\common\tools.ps1

$dotnet = Join-Path (InitializeDotNetCli $true) (GetExecutableFileName 'dotnet')
$root = Join-Path $TempDir ("publishing-tasks-test-" + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $root

try {
    foreach ($test in @(
        @{ Project = 'Publish'; Task = 'PushToBuildStorage'; Boolean = 'IsStableBuild' },
        @{ Project = 'PublishBuildAssets'; Task = 'PublishBuildToMaestro'; Boolean = 'IsAssetlessBuild' }
    )) {
        $project = GetSdkTaskProject $test.Project
        $directory = Join-Path $root $test.Project
        $null = New-Item -ItemType Directory -Path $directory
        $probePath = Join-Path $directory 'Probe.targets'
        $source = [xml](Get-Content -LiteralPath $project -Raw)
        $probe = [xml]'<Project><Target Name="TestPublishingTaskRegistration" /></Project>'
        $invocation = $source.SelectSingleNode("//$($test.Task)")
        if ($null -eq $invocation) {
            throw "Cannot find $($test.Task) in $project."
        }
        $invocation = $probe.ImportNode($invocation, $true)
        # MSBuild must resolve and load the task before rejecting this boolean.
        # Parameter binding fails before Execute, so neither task can publish.
        $invocation.SetAttribute($test.Boolean, 'NotABoolean')
        $null = $probe.Project.Target.AppendChild($invocation)
        $probe.Save($probePath)

        $arguments = @(
            'msbuild', $project, '-nologo',
            "/p:RepoRoot=$RepoRoot",
            "/p:Configuration=$configuration",
            "/p:BaseIntermediateOutputPath=$directory\",
            "/p:CustomAfterMicrosoftCommonTargets=$probePath"
        )
        & $dotnet @arguments -t:Restore -verbosity:minimal
        if ($LASTEXITCODE -ne 0) {
            throw "Restore failed for $($test.Project)."
        }
        $output = (& $dotnet @arguments -t:TestPublishingTaskRegistration -verbosity:normal 2>&1) -join "`n"
        if ($LASTEXITCODE -eq 0 -or $output -notmatch 'MSB4030' -or
            $output -notmatch 'NotABoolean' -or $output -notmatch $test.Boolean -or
            $output -match 'error MSB(?!4030|4063)\d+') {
            throw "Task registration probe failed for $($test.Project):`n$output"
        }
        $json = & $dotnet @arguments -getProperty:MicrosoftDotNetBuildTasksFeedVersion,ArcadeSdkVersion
        if ($LASTEXITCODE -ne 0) {
            throw "Version evaluation failed for $($test.Project)."
        }
        $versions = (($json -join "`n") | ConvertFrom-Json).Properties
        if ($versions.MicrosoftDotNetBuildTasksFeedVersion -ne $versions.ArcadeSdkVersion) {
            throw "$($test.Project) must use the pinned Arcade SDK's Feed task version."
        }
        $assets = Get-Content -LiteralPath (Join-Path $directory 'project.assets.json') -Raw | ConvertFrom-Json
        $package = "Microsoft.DotNet.Build.Tasks.Feed/$($versions.ArcadeSdkVersion)"
        if ($assets.libraries.PSObject.Properties.Name -notcontains $package) {
            throw "$($test.Project) did not restore $package."
        }
        Write-Host "$($test.Project): loaded $($test.Task) from Feed $($versions.MicrosoftDotNetBuildTasksFeedVersion); Execute was blocked by parameter binding."
    }

    foreach ($relativePath in @('src\package\external\external.csproj', 'scripts\build\GenerateManifest.proj')) {
        $project = Join-Path $RepoRoot $relativePath
        $version = & $dotnet msbuild $project -nologo -getProperty:MicrosoftDotNetBuildTasksFeedVersion
        if ($LASTEXITCODE -ne 0 -or ($version -join "`n").Trim() -ne '8.0.0-beta.22513.2') {
            throw "Legacy Feed version changed for $relativePath."
        }
        Write-Host "${relativePath}: legacy Feed version preserved."
    }
}
finally {
    Remove-Item -LiteralPath $root -Recurse -Force
}
