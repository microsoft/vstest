// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AdapterUtilities.Helpers
{
    using Microsoft.TestPlatform.AdapterUtilities.Resources;

    using System;
    using System.Text;

    internal static partial class ReflectionHelpers
    {
        private static void AssertSupport<T>(T obj, string methodName, string className)
            where T : class
        {
            if (obj == null)
            {
                throw new NotImplementedException(string.Format(Resources.MethodNotImplementedOnPlatform, className, methodName));
            }
        }

        internal static string ParseEscapedString(string escapedString)
        {
            var stringBuilder = new StringBuilder();
            var end = 0;
            for (int i = 0; i < escapedString.Length; i++)
            {
                if (escapedString[i] == '\'')
                {
                    stringBuilder.Append(escapedString, end, i - end);
                    end = i = ParseEscapedStringSegment(escapedString, i + 1, stringBuilder);
                }
            }

            if (stringBuilder.Length == 0)
            {
                return escapedString;
            }

            if (end != 0 && end < escapedString.Length)
            {
                stringBuilder.Append(escapedString, end, escapedString.Length - end);
            }

            return stringBuilder.ToString();
        }

        private static int ParseEscapedStringSegment(string escapedStringSegment, int pos, StringBuilder stringBuilder)
        {
            for (int i = pos; i < escapedStringSegment.Length; i++)
            {
                var c = escapedStringSegment[i];
                if (c == '\\')
                {
                    if (escapedStringSegment[i + 1] == 'u')
                    {
                        var code = escapedStringSegment.Substring(i + 2, 4);
                        c = (char)Convert.ToInt32(code, 16);
                        stringBuilder.Append(c);
                        i += 5;
                    }
                    else
                    {
                        stringBuilder.Append(escapedStringSegment[i + 1]);
                        i += 1;
                    }
                }
                else if (c == '\'')
                {
                    return i + 1;
                }
                else
                {
                    stringBuilder.Append(escapedStringSegment[i]);
                }
            }

            return escapedStringSegment.Length;
        }
    }
}
