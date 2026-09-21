// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;

internal static class CommandLineArgumentEncoder
{
    internal static string Encode(string argument)
    {
        var builder = new StringBuilder(argument.Length + 2);
        builder.Append('"');
        var backslashes = 0;
        foreach (var character in argument)
        {
            if (character == '\\')
            {
                backslashes++;
                continue;
            }

            // ProcessStartInfo.Arguments uses these quoting rules on both Windows and Unix.
            builder.Append('\\', character == '"' ? backslashes * 2 + 1 : backslashes);
            builder.Append(character);
            backslashes = 0;
        }

        builder.Append('\\', backslashes * 2);
        builder.Append('"');

        return builder.ToString();
    }
}
