// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP;

/// <summary>
/// Resolves how to launch a Microsoft.Testing.Platform (MTP) application from a test source path.
/// </summary>
internal static class MtpLaunch
{
    /// <summary>
    /// Determines the executable, base arguments and working directory used to start the MTP
    /// application for the given source. A native <c>.exe</c> source is launched directly; a managed
    /// <c>.dll</c> is launched through its sibling apphost <c>.exe</c> when present, otherwise via
    /// <c>dotnet &lt;dll&gt;</c>. The MTP server-mode switches are appended later by
    /// <see cref="PipeProtocol.TestApplication"/>.
    /// </summary>
    public static (string FileName, string Arguments, string WorkingDirectory) Resolve(string source)
    {
        string workingDirectory = Path.GetDirectoryName(source) ?? Directory.GetCurrentDirectory();
        string extension = Path.GetExtension(source);

        if (extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return (source, string.Empty, workingDirectory);
        }

        // A .NET MTP app is typically shipped as a dll with a sibling apphost .exe. Prefer the apphost
        // if present, otherwise fall back to `dotnet <dll>`.
        string apphost = Path.ChangeExtension(source, ".exe");
        return File.Exists(apphost)
            ? (apphost, string.Empty, workingDirectory)
            : ("dotnet", $"\"{source}\"", workingDirectory);
    }
}
