$ErrorActionPreference = 'Stop'
$publishingProps = Join-Path $PSScriptRoot 'Publishing.props'
$root = Join-Path ([IO.Path]::GetTempPath()) ("vstest-publishing-test-" + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $root
$packages = @(
    'Microsoft.NET.Test.Sdk.17.3.4-hotfix-test.nupkg',
    'Microsoft.TestPlatform.ObjectModel.17.3.4-hotfix-test.nupkg',
    'Microsoft.TestPlatform.Internal.Uwp.17.3.4-hotfix-test.nupkg'
)

try {
    Push-Location $root
    try {
        foreach ($configuration in @('Debug', 'Release')) {
            $directory = Join-Path $root $configuration
            $null = New-Item -ItemType Directory -Path $directory
            foreach ($name in $packages + @('Microsoft.NET.Test.Sdk.17.3.4-hotfix-test.symbols.nupkg', 'unrelated.txt')) {
                [IO.File]::WriteAllText((Join-Path $directory $name), '')
            }
            $prefix = $directory + [IO.Path]::DirectorySeparatorChar
            $json = & dotnet msbuild $publishingProps -nologo -getItem:Artifact "/p:_ServicingPackageOutDir=$prefix"
            if ($LASTEXITCODE -ne 0) {
                throw "Publishing item evaluation failed for $configuration."
            }
            $items = @((($json -join "`n") | ConvertFrom-Json).Items.Artifact)
            $actual = @($items | ForEach-Object { [IO.Path]::GetFileName($_.Identity) })
            if ($items.Count -ne 3 -or @(Compare-Object ($packages | Sort-Object) ($actual | Sort-Object)).Count -ne 0) {
                throw "Unexpected publication set for ${configuration}: $($actual -join ', ')"
            }
            foreach ($item in $items) {
                if ($item.Kind -ne 'Package') {
                    throw "Incorrect artifact kind for $($item.Identity)."
                }
                $isUwp = [IO.Path]::GetFileName($item.Identity).StartsWith('Microsoft.TestPlatform.Internal.Uwp.')
                if (($isUwp -and $item.IsShipping -ne 'false') -or (-not $isUwp -and $item.IsShipping -eq 'false')) {
                    throw "Incorrect shipping classification for $($item.Identity)."
                }
            }
            Write-Host "$configuration publication items: passed."
        }
    }
    finally {
        Pop-Location
    }
}
finally {
    Remove-Item -LiteralPath $root -Recurse -Force
}
