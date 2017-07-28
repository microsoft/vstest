// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CoreUtilities.UnitTests.Helpers
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CommandLineArgumentsHelperTests
    {
        [TestMethod]
        public void GetArgumentsDictionaryShouldReturnDictionary()
        {
            var args = new List<string>() { "--port", "12312", "--parentprocessid", "2312", "--testsourcepath", @"C:\temp\1.dll" };
            var argsDictionary = CommandLineArgumentsHelper.GetArgumentsDictionary(args.ToArray());

            Assert.AreEqual("12312", argsDictionary["--port"]);
            Assert.AreEqual("2312", argsDictionary["--parentprocessid"]);
            Assert.AreEqual(@"C:\temp\1.dll", argsDictionary["--testsourcepath"]);
        }

        [TestMethod]
        public void GetArgumentsDictionaryShouldIgnoreValuesWithoutPreceedingHypen()
        {
            var args = new List<string>() { "port", "12312", "--parentprocessid", "2312", "--testsourcepath", @"C:\temp\1.dll" };
            var argsDictionary = CommandLineArgumentsHelper.GetArgumentsDictionary(args.ToArray());

            Assert.IsTrue(argsDictionary.Count == 2);
            Assert.AreEqual("2312", argsDictionary["--parentprocessid"]);
            Assert.AreEqual(@"C:\temp\1.dll", argsDictionary["--testsourcepath"]);

            args = new List<string>() { "--port", "12312", "--parentprocessid", "2312", "testsourcepath", @"C:\temp\1.dll" };
            argsDictionary = CommandLineArgumentsHelper.GetArgumentsDictionary(args.ToArray());

            Assert.IsTrue(argsDictionary.Count == 2);
            Assert.AreEqual("12312", argsDictionary["--port"]);
            Assert.AreEqual("2312", argsDictionary["--parentprocessid"]);
        }

        [TestMethod]
        public void GetStringArgFromDictShouldReturnStringValueOrEmpty()
        {
            var args = new List<string>() { "--port", "12312", "--parentprocessid", "2312", "--testsourcepath", @"C:\temp\1.dll" };
            var argsDictionary = CommandLineArgumentsHelper.GetArgumentsDictionary(args.ToArray());

            string data = CommandLineArgumentsHelper.GetStringArgFromDict(argsDictionary, "--port");
            Assert.AreEqual("12312", data);
        }

        [TestMethod]
        public void GetStringArgFromDictShouldReturnNullIfValueIsNotPresent()
        {
            var args = new List<string>() { "--hello", "--world" };
            var argsDictionary = CommandLineArgumentsHelper.GetArgumentsDictionary(args.ToArray());

            string data = CommandLineArgumentsHelper.GetStringArgFromDict(argsDictionary, "--hello");

            Assert.IsTrue(argsDictionary.Count == 2);
            Assert.AreEqual(null, data);
        }

        [TestMethod]
        public void GetStringArgFromDictShouldReturnEmptyStringIfKeyIsNotPresent()
        {
            var args = new List<string>() { "--hello", "--world" };
            var argsDictionary = CommandLineArgumentsHelper.GetArgumentsDictionary(args.ToArray());

            string data = CommandLineArgumentsHelper.GetStringArgFromDict(argsDictionary, "--port");

            Assert.IsTrue(argsDictionary.Count == 2);
            Assert.AreEqual(string.Empty, data);
        }

        [TestMethod]
        public void GetArgumentsDictionaryShouldReturnEmptyDictionaryIfEmptyArgIsPassed()
        {
            var argsDictionary = CommandLineArgumentsHelper.GetArgumentsDictionary(null);
            Assert.IsTrue(argsDictionary.Count == 0);

            argsDictionary = CommandLineArgumentsHelper.GetArgumentsDictionary(new string[] { });
            Assert.IsTrue(argsDictionary.Count == 0);
        }

        [TestMethod]
        public void GetArgumentsDictionaryShouldTreatValueAsNullIfTwoConsecutiveKeysArePassed()
        {
            var args = new List<string>() { "--hello", "--world" };
            var argsDictionary = CommandLineArgumentsHelper.GetArgumentsDictionary(args.ToArray());

            Assert.IsTrue(argsDictionary.Count == 2);
            Assert.AreEqual(null, argsDictionary["--hello"]);
            Assert.AreEqual(null, argsDictionary["--world"]);
        }
    }
}
