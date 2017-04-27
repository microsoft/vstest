// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.UnitTests
{
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ConsoleParametersTests
    {
        [TestMethod]
        public void LogFilePathShouldEnsureDoubleQuote()
        {
            var moqFileHelper = new Mock<IFileHelper>();
            moqFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);

            var sut = new ConsoleParameters(moqFileHelper.Object);

            sut.LogFilePath = "c:\\users\\file location\\o.txt";

            string result = sut.LogFilePath;

            Assert.IsTrue(result.StartsWith("\""));
        }
    }
}
