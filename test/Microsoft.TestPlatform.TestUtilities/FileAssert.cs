// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.TestUtilities
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.IO;

    public static class FileAssert
    {
        private const string StringHighlighter = "\"\"\"";

        public static void Contains(string filePath, params string[] substrs)
        {
            Assert.IsTrue(File.Exists(filePath), $"{filePath}: file doesn't exist.");
            var fileContent= File.ReadAllText(filePath);
            foreach(var substr in substrs)
            {
                Assert.IsTrue(fileContent.Contains(substr),
                $"{filePath}: file doesn't contains {StringHighlighter} {substr} {StringHighlighter}");
            }
        }
    }
}
