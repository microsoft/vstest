// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions
{
    using System;
    using System.Net;
    using System.Text;
    using ObjectModel;

    public static class StringExtensions
    {
        /// <summary>
        /// Add double quote around string. Useful in case of path which has white space in between.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string AddDoubleQuote(this string value)
        {
            return "\"" + value + "\"";
        }

        public static void AppendToStringBuilderBasedOnMaxLength(this string data, StringBuilder result)
        {
            if (!string.IsNullOrEmpty(data))
            {
                // Don't append more data if already reached max length.
                if (result.Length >= result.MaxCapacity)
                {
                    return;
                }

                // Add newline for readbility.
                data += Environment.NewLine;

                // Append sub string of data if appending all the data exceeds max capacity.
                if (result.Length + data.Length >= result.MaxCapacity)
                {
                    data = data.Substring(0, result.MaxCapacity - result.Length);
                }

                result.Append(data);
            }
        }
    }
}
