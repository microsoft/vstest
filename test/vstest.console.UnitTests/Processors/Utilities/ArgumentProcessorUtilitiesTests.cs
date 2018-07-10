// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors.Utilities
{
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using System;
    using System.Linq;
    using TestTools.UnitTesting;

    [TestClass]
    public class ArgumentProcessorUtilitiesTests
    {
        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow(";;;;")]
        public void GetArgumentListShouldThrowErrorOnInvalidArgument(string argument)
        {
            try
            {
                ArgumemtProcessorUtilities.GetArgumentList(argument, ArgumemtProcessorUtilities.SemiColonArgumentSeperator, "exception in argument.");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.GetType().Equals(typeof(CommandLineException)));
                Assert.IsTrue(e.Message.Contains("exception in argument."));
            }
        }

        [TestMethod]
        [DataRow("abc.txt;tracelevel=info;newkey=newvalue")]
        [DataRow(";;;abc.txt;;;tracelevel=info;;;newkey=newvalue;;;;")]
        public void GetArgumentListShouldReturnCorrectArgumentList(string argument)
        {
            var argumentList = ArgumemtProcessorUtilities.GetArgumentList(argument, ArgumemtProcessorUtilities.SemiColonArgumentSeperator, "exception in argument.");
            argumentList.SequenceEqual(new string[] { "abc.txt", "tracelevel=info", "newkey=newvalue" });
        }
    }
}