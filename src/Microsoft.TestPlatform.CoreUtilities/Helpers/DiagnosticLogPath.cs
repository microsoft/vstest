// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;

internal static class DiagnosticLogPath
{
    internal static string RemoveLegacyQuotes(string path)
    {
        // Unix quotes are filename characters. On Windows, accept one legacy wrapper
        // only; embedded or additional quotes must still fail as invalid path characters.
        if (Path.DirectorySeparatorChar == '\\'
            && path.Length >= 2
            && path[0] == '"'
            && path.IndexOf('"', 1) == path.Length - 1)
        {
            return path.Substring(1, path.Length - 2);
        }

        return path;
    }
}
