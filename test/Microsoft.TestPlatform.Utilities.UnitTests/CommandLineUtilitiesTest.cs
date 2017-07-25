// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Utilities.Tests
{
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CommandLineUtilitiesTest
    {
        /// <see href="https://github.com/dotnet/roslyn/blob/614299ff83da9959fa07131c6d0ffbc58873b6ae/src/Compilers/Core/CodeAnalysisTest/CommonCommandLineParserTests.cs#L14"/>
        private void VerifyCommandLineSplitter(string commandLine, string[] expected, bool removeHashComments = false)
        {
            var actual = CommandLineUtilities.SplitCommandLineIntoArguments(commandLine, removeHashComments).ToArray();

            Assert.AreEqual(expected.Length, actual.Length);
            for (int i = 0; i < actual.Length; ++i)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        /// <see href="https://github.com/dotnet/roslyn/blob/614299ff83da9959fa07131c6d0ffbc58873b6ae/src/Compilers/Core/CodeAnalysisTest/CommonCommandLineParserTests.cs#L83"/>
        [TestMethod]
        public void TestCommandLineSplitter()
        {
            VerifyCommandLineSplitter("", new string[0]);
            VerifyCommandLineSplitter("   \t   ", new string[0]);
            VerifyCommandLineSplitter("   abc\tdef baz    quuz   ", new[] { "abc", "def", "baz", "quuz" });
            VerifyCommandLineSplitter(@"  ""abc def""  fi""ddle dee de""e  ""hi there ""dude  he""llo there""  ",
                                        new string[] { @"abc def", @"fi""ddle dee de""e", @"""hi there ""dude", @"he""llo there""" });
            VerifyCommandLineSplitter(@"  ""abc def \"" baz quuz"" ""\""straw berry"" fi\""zz \""buzz fizzbuzz",
                                        new string[] { @"abc def \"" baz quuz", @"\""straw berry", @"fi\""zz", @"\""buzz", @"fizzbuzz" });
            VerifyCommandLineSplitter(@"  \\""abc def""  \\\""abc def"" ",
                                        new string[] { @"\\""abc def""", @"\\\""abc", @"def"" " });
            VerifyCommandLineSplitter(@"  \\\\""abc def""  \\\\\""abc def"" ",
                                        new string[] { @"\\\\""abc def""", @"\\\\\""abc", @"def"" " });
            VerifyCommandLineSplitter(@"  \\\\""abc def""  \\\\\""abc def"" q a r ",
                                        new string[] { @"\\\\""abc def""", @"\\\\\""abc", @"def"" q a r " });
            VerifyCommandLineSplitter(@"abc #Comment ignored",
                                        new string[] { @"abc" }, removeHashComments: true);
            VerifyCommandLineSplitter(@"""foo bar"";""baz"" ""tree""",
                                        new string[] { @"""foo bar"";""baz""", "tree" });
            VerifyCommandLineSplitter(@"/reference:""a, b"" ""test""",
                                        new string[] { @"/reference:""a, b""", "test" });
            VerifyCommandLineSplitter(@"fo""o ba""r",
                                        new string[] { @"fo""o ba""r" });
        }
    }
}
