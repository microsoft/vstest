$root = Resolve-Path "$PSScriptRoot/.."

$source = "S:\c\NuGet.Client"

$destination = "$root/src/Microsoft.TestPlatform.ObjectModel/Nuget.Frameworks/"

$frameworksPath = "$source/src/NuGet.Core/NuGet.Frameworks"
$frameworkItems = @(
    "Strings.Designer.cs"
    "DefaultFrameworkMappings.cs"
    "DefaultFrameworkNameProvider.cs"
    "DefaultPortableFrameworkMappings.cs"
    "DefaultCompatibilityProvider.cs"
    "CompatibilityProvider.cs"
    "FrameworkConstants.cs"
    "FrameworkException.cs"
    "FrameworkNameProvider.cs"
    "FrameworkRange.cs"
    "FrameworkReducer.cs"
    "FrameworkNameHelpers.cs",
    "FrameworkSpecificMapping.cs"
    "FallbackFramework.cs"
    "FrameworkExpander.cs"
    "CompatibilityCacheKey.cs"
    "def/IFrameworkCompatibilityListProvider.cs"
    "def/IFrameworkCompatibilityProvider.cs"
    "def/IFrameworkMappings.cs"
    "def/IFrameworkNameProvider.cs"
    "def/IFrameworkSpecific.cs"
    "def/IPortableFrameworkMappings.cs"
    "NuGetFramework.cs"
    "NuGetFrameworkFactory.cs"
    "comparers/NuGetFrameworkFullComparer.cs"
    "comparers/NuGetFrameworkNameComparer.cs"
    "comparers/CompatibilityMappingComparer.cs"
    "comparers/FrameworkRangeComparer.cs"
    "comparers/NuGetFrameworkSorter.cs"
    "comparers/FrameworkPrecedenceSorter.cs"
    "NuGetFrameworkUtility.cs"
    "OneWayCompatibilityMappingEntry.cs"
) | ForEach-Object { "$frameworksPath/$_" }

$extraItems = @(
    ".editorconfig"
    "build/Shared/HashCodeCombiner.cs"
    "build/Shared/NoAllocEnumerateExtensions.cs"
    "build/Shared/StringBuilderPool.cs"
    "build/Shared/SimplePool.cs"
) | ForEach-Object { "$source/$_" }

if ((Test-Path $destination)) { 
    Remove-Item $destination -Force -Recurse
}

New-Item -ItemType Directory $destination -ErrorAction Ignore | Out-Null
foreach ($item in $frameworkItems + $extraItems) {
    if (-not (Test-Path $item)) { 
        throw "File not found $item"
    }
    $content = Get-Content $item
    $name = (Get-Item $item).Name

    $path = "$destination/$name"

    # some types are directly in Nuget namespace, and if we would suffix
    # .Clone, then Nuget.Frameworks.Clone is no longer autometicaly using
    # Nuget.Clone, and we would have to add more usings into the files.
    $finalContent = $content `
        -replace 'public(.*)(class|interface)', 'internal$1$2' `
        -replace 'namespace NuGet', 'namespace NuGetClone' `
        -replace 'using NuGet', 'using NuGetClone' `
        -replace 'NuGet.Frameworks.NuGetFramework', 'NuGetClone.Frameworks.NuGetFramework'

    if ($name -eq ".editorconfig") {
        $finalContent += @"

[*.{cs,vb}]
dotnet_diagnostic.IDE0001.severity = none
dotnet_diagnostic.IDE0005.severity = none
dotnet_diagnostic.IDE1006.severity = none
"@
    }
    $finalContent | Set-Content -Path $path -Encoding utf8NoBOM
}