// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.TestHostProvider.Hosting.UnitTests
{
    using System;
    using System.Text;
    using Microsoft.TestPlatform.TestHostProvider.Hosting;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestHostManagerCallbacksTests
    {
        private StringBuilder testHostProcessStdError;

        public TestHostManagerCallbacksTests()
        {
            testHostProcessStdError = new StringBuilder(0, Microsoft.VisualStudio.TestPlatform.CoreUtilities.Constants.StandardErrorMaxLength);
        }

        [TestMethod]
        public void ErrorReceivedCallbackShouldAppendNoDataOnNullDataReceived()
        {
            testHostProcessStdError.Append("NoDataShouldAppend");
            TestHostManagerCallbacks.ErrorReceivedCallback(testHostProcessStdError, null);

            Assert.AreEqual("NoDataShouldAppend", testHostProcessStdError.ToString());
        }

        [TestMethod]
        public void ErrorReceivedCallbackShouldAppendNoDataOnEmptyDataReceived()
        {
            testHostProcessStdError.Append("NoDataShouldAppend");
            TestHostManagerCallbacks.ErrorReceivedCallback(testHostProcessStdError, string.Empty);

            Assert.AreEqual("NoDataShouldAppend", testHostProcessStdError.ToString());
        }

        [TestMethod]
        public void ErrorReceivedCallbackShouldAppendWhiteSpaceString()
        {
            testHostProcessStdError.Append("OldData");
            TestHostManagerCallbacks.ErrorReceivedCallback(testHostProcessStdError, " ");

            Assert.AreEqual("OldData " + Environment.NewLine, testHostProcessStdError.ToString());
        }

        [TestMethod]
        public void ErrorReceivedCallbackShouldAppendGivenData()
        {
            testHostProcessStdError.Append("NoDataShouldAppend");
            TestHostManagerCallbacks.ErrorReceivedCallback(testHostProcessStdError, "new data");

            Assert.AreEqual("NoDataShouldAppendnew data" + Environment.NewLine, testHostProcessStdError.ToString());
        }

        [TestMethod]
        public void ErrorReceivedCallbackShouldNotAppendNewDataIfErrorMessageAlreadyReachedMaxLength()
        {
            testHostProcessStdError = new StringBuilder(0, 5);
            testHostProcessStdError.Append("12345");
            TestHostManagerCallbacks.ErrorReceivedCallback(testHostProcessStdError, "678");

            Assert.AreEqual("12345", testHostProcessStdError.ToString());
        }

        [TestMethod]
        public void ErrorReceivedCallbackShouldAppendSubStringOfDataIfErrorMessageReachedMaxLength()
        {
            testHostProcessStdError = new StringBuilder(0, 5);
            testHostProcessStdError.Append("1234");
            TestHostManagerCallbacks.ErrorReceivedCallback(testHostProcessStdError, "5678");

            Assert.AreEqual("12345", testHostProcessStdError.ToString());
        }

        [TestMethod]
        public void ErrorReceivedCallbackShouldAppendEntireStringEvenItReachesToMaxLength()
        {
            testHostProcessStdError = new StringBuilder(0, 5);
            testHostProcessStdError.Append("12");
            TestHostManagerCallbacks.ErrorReceivedCallback(testHostProcessStdError, "3");

            Assert.AreEqual("123" + Environment.NewLine, testHostProcessStdError.ToString());
        }

        [TestMethod]
        public void ErrorReceivedCallbackShouldAppendNewLineApproprioritlyWhenReachingMaxLength()
        {
            testHostProcessStdError = new StringBuilder(0, 5);
            testHostProcessStdError.Append("123");
            TestHostManagerCallbacks.ErrorReceivedCallback(testHostProcessStdError, "4");

            Assert.AreEqual("1234" + Environment.NewLine.Substring(0, 1), testHostProcessStdError.ToString());
        }
    }
}
