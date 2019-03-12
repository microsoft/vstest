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
            this.testHostProcessStdError = new StringBuilder(0, Microsoft.VisualStudio.TestPlatform.CoreUtilities.Constants.StandardErrorMaxLength);
        }

        [TestMethod]
        public void ErrorReceivedCallbackShouldAppendNoDataOnNullDataReceived()
        {
            this.testHostProcessStdError.Append("NoDataShouldAppend");
            TestHostManagerCallbacks.ErrorReceivedCallback(this.testHostProcessStdError, null);

            Assert.AreEqual("NoDataShouldAppend", this.testHostProcessStdError.ToString());
        }

        [TestMethod]
        public void ErrorReceivedCallbackShouldAppendNoDataOnEmptyDataReceived()
        {
            this.testHostProcessStdError.Append("NoDataShouldAppend");
            TestHostManagerCallbacks.ErrorReceivedCallback(this.testHostProcessStdError, string.Empty);

            Assert.AreEqual("NoDataShouldAppend", this.testHostProcessStdError.ToString());
        }

        [TestMethod]
        public void ErrorReceivedCallbackShouldAppendWhiteSpaceString()
        {
            this.testHostProcessStdError.Append("OldData");
            TestHostManagerCallbacks.ErrorReceivedCallback(this.testHostProcessStdError, " ");

            Assert.AreEqual("OldData " + Environment.NewLine, this.testHostProcessStdError.ToString());
        }

        [TestMethod]
        public void ErrorReceivedCallbackShouldAppendGivenData()
        {
            this.testHostProcessStdError.Append("NoDataShouldAppend");
            TestHostManagerCallbacks.ErrorReceivedCallback(this.testHostProcessStdError, "new data");

            Assert.AreEqual("NoDataShouldAppendnew data" + Environment.NewLine, this.testHostProcessStdError.ToString());
        }

        [TestMethod]
        public void ErrorReceivedCallbackShouldNotAppendNewDataIfErrorMessageAlreadyReachedMaxLength()
        {
            this.testHostProcessStdError = new StringBuilder(0, 5);
            this.testHostProcessStdError.Append("12345");
            TestHostManagerCallbacks.ErrorReceivedCallback(this.testHostProcessStdError, "678");

            Assert.AreEqual("12345", this.testHostProcessStdError.ToString());
        }

        [TestMethod]
        public void ErrorReceivedCallbackShouldAppendSubStringOfDataIfErrorMessageReachedMaxLength()
        {
            this.testHostProcessStdError = new StringBuilder(0, 5);
            this.testHostProcessStdError.Append("1234");
            TestHostManagerCallbacks.ErrorReceivedCallback(this.testHostProcessStdError, "5678");

            Assert.AreEqual("12345", this.testHostProcessStdError.ToString());
        }

        [TestMethod]
        public void ErrorReceivedCallbackShouldAppendEntireStringEvenItReachesToMaxLength()
        {
            this.testHostProcessStdError = new StringBuilder(0, 5);
            this.testHostProcessStdError.Append("12");
            TestHostManagerCallbacks.ErrorReceivedCallback(this.testHostProcessStdError, "3");

            Assert.AreEqual("123" + Environment.NewLine, this.testHostProcessStdError.ToString());
        }

        [TestMethod]
        public void ErrorReceivedCallbackShouldAppendNewLineApproprioritlyWhenReachingMaxLength()
        {
            this.testHostProcessStdError = new StringBuilder(0, 5);
            this.testHostProcessStdError.Append("123");
            TestHostManagerCallbacks.ErrorReceivedCallback(this.testHostProcessStdError, "4");

            Assert.AreEqual("1234" + Environment.NewLine.Substring(0, 1), this.testHostProcessStdError.ToString());
        }
    }
}
