// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.RegularExpressions;

using static System.Console;
using static System.ConsoleColor;

namespace Intent.Console;

internal class ConsoleLogger : IRunLogger
{
    public void WriteTestInconclusive(MethodInfo m)
    {
        var currentColor = ForegroundColor;
        ForegroundColor = Yellow;
        WriteLine($"[?] {FormatMethodName(m.Name)}");
        ForegroundColor = currentColor;
    }

    public void WriteTestPassed(MethodInfo m)
    {
        var currentColor = ForegroundColor;
        ForegroundColor = Green;
        WriteLine($"[+] {FormatMethodName(m.Name)}");
        ForegroundColor = currentColor;
    }

    public void WriteTestFailure(MethodInfo m, Exception ex)
    {
        var currentColor = ForegroundColor;
        ForegroundColor = Red;
        WriteLine($"[-] {FormatMethodName(m.Name)}{Environment.NewLine}{ex}");
        ForegroundColor = currentColor;
    }

    public void WriteFrameworkError(Exception ex)
    {
        var currentColor = ForegroundColor;
        ForegroundColor = DarkRed;
        WriteLine($"[-] framework failed{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
        ForegroundColor = currentColor;
    }

    private static string FormatMethodName(string methodName)
    {
        var noUnderscores = methodName.Replace('_', ' ');
        // insert space before every capital letter or number that is after a non-capital letter
        var spaced = Regex.Replace(noUnderscores, "(?<=[a-z])([A-Z0-9])", " $1");
        // insert space before every capital leter that is after a number
        var spaced2 = Regex.Replace(spaced, "(?<=[0-9]|^)([A-Z])", " $1");
        var newLines = spaced2.Replace("When", $"{Environment.NewLine}     When")
            .Replace("Then", $"{Environment.NewLine}     Then");

        return newLines.ToLowerInvariant();
    }
}
