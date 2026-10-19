// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETCOREAPP

using System;
using System.Text;

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization;

/// <summary>
/// Repairs JSON text that contains unpaired UTF-16 surrogate escapes, which
/// <see cref="System.Text.Json.JsonSerializer"/> refuses to read.
/// </summary>
/// <remarks>
/// .NET Framework testhosts serialize with Jsonite, which escapes every surrogate code unit on its
/// own (an emoji is written as <c>\uD83D\uDE00</c>). A test display name that was truncated in the
/// middle of an astral character - xUnit for example truncates long theory arguments by UTF-16
/// length - therefore arrives as a lone <c>\uD83D</c> escape. System.Text.Json rejects that by
/// design and aborts the whole run, so we replace the unpaired code unit with U+FFFD REPLACEMENT
/// CHARACTER before handing the text to the reader.
/// </remarks>
internal static class JsonSurrogates
{
    private const string ReplacementEscape = "\\uFFFD";

    /// <summary>
    /// Replaces every unpaired surrogate escape in <paramref name="json"/> with an escaped U+FFFD.
    /// Returns the original instance when there is nothing to repair.
    /// </summary>
    internal static string ReplaceUnpaired(string json)
    {
        // Surrogates can only reach us as escapes: the message is decoded from UTF-8, which cannot
        // carry a lone surrogate, and the serializers that escape surrogates always escape both
        // halves. So no "\u" at all means there is nothing to repair. IndexOf is vectorized, which
        // keeps the overhead negligible for the vast majority of messages that are plain ASCII.
        if (json.IndexOf("\\u", StringComparison.Ordinal) < 0)
        {
            return json;
        }

        // Stays null until the first replacement, so messages that merely contain valid escapes
        // (control characters, complete surrogate pairs) are returned without allocating.
        StringBuilder? builder = null;

        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];
            if (c != '\\')
            {
                builder?.Append(c);
                continue;
            }

            if (!TryReadUnicodeEscape(json, i, out char escaped))
            {
                // Some other escape, e.g. \\ or \". Copy it as a whole so that the second backslash
                // of an escaped backslash is not mistaken for the start of the next escape.
                builder?.Append(c);
                if (i + 1 < json.Length)
                {
                    builder?.Append(json[i + 1]);
                    i++;
                }

                continue;
            }

            if (char.IsHighSurrogate(escaped)
                && TryReadUnicodeEscape(json, i + 6, out char low)
                && char.IsLowSurrogate(low))
            {
                // A complete pair, keep both escapes and skip past them.
                builder?.Append(json, i, 12);
                i += 11;
                continue;
            }

            if (char.IsSurrogate(escaped))
            {
                builder ??= new StringBuilder(json.Length).Append(json, 0, i);
                builder.Append(ReplacementEscape);
            }
            else
            {
                builder?.Append(json, i, 6);
            }

            i += 5;
        }

        return builder?.ToString() ?? json;
    }

    private static bool TryReadUnicodeEscape(string json, int index, out char value)
    {
        value = default;

        if (index < 0 || index + 5 >= json.Length || json[index] != '\\' || json[index + 1] != 'u')
        {
            return false;
        }

        int codeUnit = 0;
        for (int i = index + 2; i <= index + 5; i++)
        {
            int digit = HexValue(json[i]);
            if (digit < 0)
            {
                return false;
            }

            codeUnit = (codeUnit << 4) | digit;
        }

        value = (char)codeUnit;
        return true;
    }

    private static int HexValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1,
    };
}

#endif
