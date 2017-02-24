// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.UnitTests.Utility
{
    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TrxLoggerOutcome = Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel.TestOutcome;

    [TestClass]
    public class ConverterTests
    {
        [TestMethod]
        public void ToOutcomeShouldMapFailedToFailed()
        {
            Assert.AreEqual(TrxLoggerOutcome.Failed, Converter.ToOutcome(TestOutcome.Failed));
        }

        [TestMethod]
        public void ToOutcomeShouldMapPassedToPassed()
        {
            Assert.AreEqual(TrxLoggerOutcome.Passed, Converter.ToOutcome(TestOutcome.Passed));
        }

        [TestMethod]
        public void ToOutcomeShouldMapSkippedToNotExecuted()
        {
            Assert.AreEqual(TrxLoggerOutcome.NotExecuted, Converter.ToOutcome(TestOutcome.Skipped));
        }

        [TestMethod]
        public void ToOutcomeShouldMapNoneToNotExecuted()
        {
            Assert.AreEqual(TrxLoggerOutcome.NotExecuted, Converter.ToOutcome(TestOutcome.None));
        }

        [TestMethod]
        public void ToOutcomeShouldMapNotFoundToNotExecuted()
        {
            Assert.AreEqual(TrxLoggerOutcome.NotExecuted, Converter.ToOutcome(TestOutcome.NotFound));
        }
    }
}
