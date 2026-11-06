$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$dotnet = Join-Path $repo '.dotnet\dotnet.exe'
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('vstest-cold-references-' + [Guid]::NewGuid().ToString('N'))
$previousPackages = $env:NUGET_PACKAGES
$packages = if ($previousPackages) { $previousPackages } else { Join-Path $env:USERPROFILE '.nuget\packages' }
$projects = @('LegacySettingsUnitTestProject', 'MstestV1UnitTestProject', 'CUITTestProject', 'LocalCodedWebTest')
try {
    New-Item -ItemType Directory -Path $temporary | Out-Null
    foreach ($project in $projects) {
        foreach ($suffix in @('', '\')) {
            $env:NUGET_PACKAGES = $packages.TrimEnd('\', '/') + $suffix
            $extensions = Join-Path $temporary $project
            $projectFile = Join-Path $repo "test\TestAssets\$project\$project.csproj"
            $json = & $dotnet msbuild $projectFile -nologo -p:TargetFramework=net451 "-p:MSBuildProjectExtensionsPath=$extensions\" -getItem:Reference
            if ($LASTEXITCODE -ne 0) { throw "Could not evaluate cold references for $project." }
            $references = @(
                ($json | ConvertFrom-Json).Items.Reference |
                    Where-Object { $_.Identity -like 'Microsoft.VisualStudio.*' }
            )
            if ($references.Count -eq 0) { throw "No legacy references found in $project." }
            foreach ($reference in $references) {
                $expected = Join-Path $env:NUGET_PACKAGES "microsoft.testplatform\15.9.2\tools\net451\Common7\IDE\Extensions\TestPlatform\$($reference.Identity).dll"
                if ([IO.Path]::GetFullPath($reference.HintPath) -cne [IO.Path]::GetFullPath($expected)) {
                    throw "Cold evaluation selected an incorrect reference: $($reference.HintPath); expected $expected."
                }
                if (-not (Test-Path -LiteralPath $reference.HintPath -PathType Leaf)) {
                    throw "The declared 15.9 reference is missing: $($reference.HintPath)."
                }
            }
        }
    }
    Write-Host 'Cold asset-reference tests passed for all four fixtures, with and without a cache trailing separator.'
}
finally {
    $env:NUGET_PACKAGES = $previousPackages
    if (Test-Path -LiteralPath $temporary) {
        Remove-Item -LiteralPath $temporary -Recurse -Force
    }
}
