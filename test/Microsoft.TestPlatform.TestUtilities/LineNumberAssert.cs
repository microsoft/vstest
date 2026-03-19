// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.TestUtilities;

/// <summary>
/// Assertions for line numbers reported by source navigation (e.g. DiaSession).
/// Instead of hardcoding exact line numbers that differ between debug and release builds,
/// these helpers read the actual source file and verify the reported line falls within
/// the range of the named method, using the method declaration as a natural marker.
/// </summary>
public static class LineNumberAssert
{
    /// <summary>
    /// Asserts that <paramref name="reportedLine"/> falls within the source range of any overload
    /// of <paramref name="methodName"/> in <paramref name="sourceFile"/>.
    /// </summary>
    public static void AssertIsWithinMethod(string? sourceFile, string methodName, int reportedLine)
    {
        Assert.IsNotNull(sourceFile, $"Source file path is null while asserting line number for method '{methodName}'.");

        var ranges = FindMethodRanges(sourceFile, methodName);

        Assert.IsTrue(ranges.Count > 0, $"Method '{methodName}' not found in file '{sourceFile}'.");

        bool isWithin = ranges.Any(r => reportedLine >= r.Start && reportedLine <= r.End);

        if (!isWithin)
        {
            var rangesStr = string.Join(", ", ranges.Select(r => $"[{r.Start}-{r.End}]"));
            Assert.Fail($"Expected reported line {reportedLine} to be within method '{methodName}' in '{sourceFile}'. Method range(s): {rangesStr}.");
        }
    }

    /// <summary>
    /// Asserts that both <paramref name="minLine"/> and <paramref name="maxLine"/> fall within the
    /// source range of any overload of <paramref name="methodName"/> in <paramref name="sourceFile"/>.
    /// </summary>
    public static void AssertRangeIsWithinMethod(string? sourceFile, string methodName, int minLine, int maxLine)
    {
        AssertIsWithinMethod(sourceFile, methodName, minLine);
        AssertIsWithinMethod(sourceFile, methodName, maxLine);
    }

    private readonly record struct LineRange(int Start, int End);

    private static List<LineRange> FindMethodRanges(string sourceFile, string methodName)
    {
        var lines = File.ReadAllLines(sourceFile);
        var ranges = new List<LineRange>();

        for (int i = 0; i < lines.Length; i++)
        {
            if (!IsMethodDeclaration(lines[i], methodName))
                continue;

            // Scan backwards to include any attribute lines that belong to this method.
            int startLine = i;
            while (startLine > 0 && IsAttributeLine(lines[startLine - 1]))
                startLine--;

            // Scan forward to find the matching closing brace.
            int depth = 0;
            int endLine = i;
            bool foundBody = false;

            for (int j = i; j < lines.Length; j++)
            {
                CountBraces(lines[j], ref depth, ref foundBody);

                if (foundBody && depth == 0)
                {
                    endLine = j;
                    break;
                }
            }

            // Convert from 0-based index to 1-based line number.
            ranges.Add(new LineRange(startLine + 1, endLine + 1));
        }

        return ranges;
    }

    private static bool IsMethodDeclaration(string line, string methodName)
    {
        var trimmed = line.TrimStart();

        // Skip comments and documentation lines.
        if (trimmed.StartsWith("//", StringComparison.Ordinal)
            || trimmed.StartsWith("*", StringComparison.Ordinal)
            || trimmed.StartsWith("/*", StringComparison.Ordinal))
        {
            return false;
        }

        // Find "MethodName(" in the line.
        int idx = trimmed.IndexOf(methodName + "(", StringComparison.Ordinal);
        if (idx < 0)
            return false;

        // The character immediately before the method name must not be an identifier character
        // or a dot (to exclude calls like "obj.MethodName(" or "MyMethodName(").
        if (idx > 0)
        {
            char prev = trimmed[idx - 1];
            if (char.IsLetterOrDigit(prev) || prev == '_' || prev == '.')
                return false;
        }

        return true;
    }

    private static bool IsAttributeLine(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("[", StringComparison.Ordinal);
    }

    /// <summary>
    /// Counts unescaped braces in <paramref name="line"/>, ignoring those inside
    /// string literals, character literals, and single-line comments.
    /// </summary>
    private static void CountBraces(string line, ref int depth, ref bool foundBody)
    {
        bool inString = false;
        bool inChar = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            // Detect start of a line comment; stop processing the rest of the line.
            if (!inString && !inChar && c == '/' && i + 1 < line.Length && line[i + 1] == '/')
                break;

            if (!inChar && c == '"')
            {
                // Handle verbatim strings @"..." - still treat as toggling inString.
                if (!inString)
                {
                    inString = true;
                }
                else
                {
                    // Check for escaped quote inside string ("").
                    if (i + 1 < line.Length && line[i + 1] == '"')
                        i++; // skip the second quote of an escaped pair
                    else
                        inString = false;
                }

                continue;
            }

            if (!inString && c == '\'')
            {
                inChar = !inChar;
                continue;
            }

            if (!inString && !inChar)
            {
                if (c == '{') { depth++; foundBody = true; }
                else if (c == '}') depth--;
            }
        }
    }
}
