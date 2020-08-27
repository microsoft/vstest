<# Helps updating framework name translation in Framework class by taking values from Nuget.Frameworks package
and generating all common frameworks into the types that vstest console uses. This removes the dependency on Nuget.Frameworks
from which we consume miniscule part and which causes us a lot of trouble because the VS version and local version differ
#>

# download a version from nuget and unpack

$dll = Get-Item "~/Downloads/nuget.frameworks.*\lib\net472\NuGet.Frameworks.dll"

Import-Module ($dll.FullName)

$commonFrameworks = [NuGet.Frameworks.FrameworkConstants+CommonFrameworks].GetFields().Name

# thie generates mapping from the full name to the one that we use internally
@"
// Generated from Nuget.Frameworks $((Get-Module NuGet.Frameworks ).Version) nuget package, 
// you can update it by scripts/generate/update-supported-nuget.frameworks-versions.ps1
static Dictionary<string, Framework> mapping = new Dictionary<string, Framework> {
$(foreach ($commonFramework in $commonFrameworks) { 

    $fmw = [NuGet.Frameworks.FrameworkConstants+CommonFrameworks]::$commonFramework
    "[""$($fmw.DotNetFrameworkName)""] = new Framework { 
        Name = ""$($fmw.DotNetFrameworkName)"",
        FrameworkName = ""$($fmw.Framework)"",
        Version = ""$($fmw.Version)"",
        ShortName = ""$($fmw.GetShortFolderName())"",
    },`n"

})
};
"@

# this will help you taking the method TryParseCommonFramework from the main branch and updating it to have the uncommon name mappings
# but not use the frameworkconstants
# Take TryParseCommonFramework from https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.Frameworks/NuGetFrameworkFactory.cs#L586
# and put it below, in betwwen the @' '@ to get a version that does not need any built in types
@'
private static bool TryParseCommonFramework(string frameworkString, out NuGetFramework framework)
        {
            framework = null;

            frameworkString = frameworkString.ToLowerInvariant();

            switch (frameworkString)
            {
                case "dotnet":
                case "dotnet50":
                case "dotnet5.0":
                    framework = FrameworkConstants.CommonFrameworks.DotNet50;
                    break;
                case "net40":
                case "net4":
                    framework = FrameworkConstants.CommonFrameworks.Net4;
                    break;
                case "net45":
                    framework = FrameworkConstants.CommonFrameworks.Net45;
                    break;
                case "net451":
                    framework = FrameworkConstants.CommonFrameworks.Net451;
                    break;
                case "net46":
                    framework = FrameworkConstants.CommonFrameworks.Net46;
                    break;
                case "net461":
                    framework = FrameworkConstants.CommonFrameworks.Net461;
                    break;
                case "net462":
                    framework = FrameworkConstants.CommonFrameworks.Net462;
                    break;
                case "win8":
                    framework = FrameworkConstants.CommonFrameworks.Win8;
                    break;
                case "win81":
                    framework = FrameworkConstants.CommonFrameworks.Win81;
                    break;
                case "netstandard":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard;
                    break;
                case "netstandard1.0":
                case "netstandard10":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard10;
                    break;
                case "netstandard1.1":
                case "netstandard11":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard11;
                    break;
                case "netstandard1.2":
                case "netstandard12":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard12;
                    break;
                case "netstandard1.3":
                case "netstandard13":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard13;
                    break;
                case "netstandard1.4":
                case "netstandard14":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard14;
                    break;
                case "netstandard1.5":
                case "netstandard15":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard15;
                    break;
                case "netstandard1.6":
                case "netstandard16":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard16;
                    break;
                case "netstandard1.7":
                case "netstandard17":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard17;
                    break;
                case "netstandard2.0":
                case "netstandard20":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard20;
                    break;
                case "netstandard2.1":
                case "netstandard21":
                    framework = FrameworkConstants.CommonFrameworks.NetStandard21;
                    break;
                case "netcoreapp2.1":
                case "netcoreapp21":
                    framework = FrameworkConstants.CommonFrameworks.NetCoreApp21;
                    break;
                case "netcoreapp3.1":
                case "netcoreapp31":
                    framework = FrameworkConstants.CommonFrameworks.NetCoreApp31;
                    break;
                case "netcoreapp5.0":
                case "netcoreapp50":
                case "net5.0":
                case "net50":
                    framework = FrameworkConstants.CommonFrameworks.Net50;
                    break;
            }

            return framework != null;
        }
          
'@ -split "`n" | foreach { 
    $pattern = "FrameworkConstants.CommonFrameworks.(?<framework>.*);"
    if ($_ -match $pattern) { 
        $name = $matches.framework
        $fullName = ([Nuget.Frameworks.FrameworkConstants+CommonFrameworks]::$name).DotNetFrameworkName
        $_ -replace $pattern, """$fullName"";"
    }
    else {
        $_ -replace "out NuGetFramework framework", "out string framework"
    }
}