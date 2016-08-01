// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.Common.UnitTests.Utilities
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ExceptionUtilitiesTests
    {
        [TestMethod]
        public void GetExceptionMessageShouldReturnEmptyIfExceptionIsNull()
        {
            Assert.AreEqual(string.Empty, ExceptionUtilities.GetExceptionMessage(null));
        }

        [TestMethod]
        public void GetExceptionMessageShouldReturnExceptionMessage()
        {
            var exception = new ArgumentException("Some bad stuff");
            Assert.AreEqual(exception.Message, ExceptionUtilities.GetExceptionMessage(exception));
        }

        [TestMethod]
        public void GetExceptionMessageShouldReturnFormattedExceptionMessageWithInnerExceptionDetails()
        {
            var innerException = new Exception("Bad stuff internally");
            var innerException2 = new Exception("Bad stuff internally 2", innerException);
            var exception = new ArgumentException("Some bad stuff", innerException2);
            var expectedMessage = exception.Message + Environment.NewLine + innerException2.Message
                                  + Environment.NewLine + innerException.Message; 
            Assert.AreEqual(expectedMessage, ExceptionUtilities.GetExceptionMessage(exception));
        }
    }
}
