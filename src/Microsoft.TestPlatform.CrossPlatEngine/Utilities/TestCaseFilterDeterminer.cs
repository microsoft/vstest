// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Utilities
{
    internal class TestCaseFilterDeterminer
    {
        internal static string ShortenTestCaseFilterIfRequired(string testCaseFilter)
        {
            var maxLength = 256;
            string shortenTestCaseFilter;

            if (testCaseFilter.Length > maxLength)
            {
                shortenTestCaseFilter = testCaseFilter.Substring(0, maxLength) + "...";
            }
            else
            {
                shortenTestCaseFilter = testCaseFilter;
            }

            return shortenTestCaseFilter;
        }
    }
}
