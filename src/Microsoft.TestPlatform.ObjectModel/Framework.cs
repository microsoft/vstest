// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD1_0
using NuGet.Frameworks;

using static NuGet.Frameworks.FrameworkConstants;
#endif

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel;

/// <summary>
/// Class for target Framework for the test container
/// </summary>
public class Framework
{
    private Framework()
    {
    }

    /// <summary>
    /// Default .Net target framework.
    /// </summary>
    public static Framework DefaultFramework { get; }
#if NETFRAMEWORK
        = Framework.FromString(".NETFramework,Version=v4.0");
#elif !NETSTANDARD1_0
        = Framework.FromString(".NETCoreApp,Version=v1.0");
#endif

    /// <summary>
    /// Gets the FullName of framework.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the framework version.
    /// </summary>
    public string Version { get; private set; }

    /// <summary>
    /// Returns a valid framework else returns null
    /// </summary>
    /// <param name="frameworkString">Framework name</param>
    /// <returns>A framework object</returns>
    public static Framework FromString(string frameworkString)
    {
#if NETSTANDARD1_0
#pragma warning disable IDE1006 // Naming Styles
        var CommonFrameworks = new
#pragma warning restore IDE1006 // Naming Styles
        {
            Net35 = new { DotNetFrameworkName = Constants.DotNetFramework35, Version = "3.5.0.0" },
            Net4 = new { DotNetFrameworkName = Constants.DotNetFramework40, Version = "4.0.0.0" },
            Net45 = new { DotNetFrameworkName = Constants.DotNetFramework45, Version = "4.5.0.0" },
            NetCoreApp10 = new { DotNetFrameworkName = Constants.DotNetFrameworkCore10, Version = "1.0.0.0" },
            UAP10 = new { DotNetFrameworkName = Constants.DotNetFrameworkUap10, Version = "10.0.0.0" },
        };
#endif

        if (string.IsNullOrWhiteSpace(frameworkString))
        {
            return null;
        }

        string name, version;
        try
        {
            // IDE always sends framework in form of ENUM, which always throws exception
            // This throws up in first chance exception, refer Bug https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems/edit/591142
            switch (frameworkString.Trim().ToLower())
            {
                case "framework35":
                    name = CommonFrameworks.Net35.DotNetFrameworkName;
                    version = CommonFrameworks.Net35.Version.ToString();
                    break;

                case "framework40":
                    name = CommonFrameworks.Net4.DotNetFrameworkName;
                    version = CommonFrameworks.Net4.Version.ToString();
                    break;

                case "framework45":
                    name = CommonFrameworks.Net45.DotNetFrameworkName;
                    version = CommonFrameworks.Net45.Version.ToString();
                    break;

                case "frameworkcore10":
                    name = CommonFrameworks.NetCoreApp10.DotNetFrameworkName;
                    version = CommonFrameworks.NetCoreApp10.Version.ToString();
                    break;

                case "frameworkuap10":
                    name = CommonFrameworks.UAP10.DotNetFrameworkName;
                    version = CommonFrameworks.UAP10.Version.ToString();
                    break;

                default:
#if NETSTANDARD1_0
                    return null;
#else
                    var nugetFramework = NuGetFramework.Parse(frameworkString);
                    if (nugetFramework.IsUnsupported)
                        return null;

                    name = nugetFramework.DotNetFrameworkName;
                    version = nugetFramework.Version.ToString();

                    break;
#endif
            }
        }
        catch
        {
            return null;
        }

        return new Framework() { Name = name, Version = version };
    }

    /// <summary>
    /// Returns full name of the framework.
    /// </summary>
    /// <returns>String presentation of the object.</returns>
    public override string ToString()
    {
        return Name;
    }
}
