// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.DataCollection.V1
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging.Events;
    using Microsoft.VisualStudio.TestTools.Common;

    using DataCollectionContext = Microsoft.VisualStudio.TestTools.Execution.DataCollectionContext;
    using DataCollectionEnvironmentContext = Microsoft.VisualStudio.TestTools.Execution.DataCollectionEnvironmentContext;
    using SessionEndEventArgs = Microsoft.VisualStudio.TestTools.Execution.SessionEndEventArgs;
    using SessionStartEventArgs = Microsoft.VisualStudio.TestTools.Execution.SessionStartEventArgs;

    /// <summary>
    /// Data collector understands test tools object as they are written with MSTest interface.
    /// Implements helper methods to converts test platform objects to test tools object so they
    /// can be passed to data collectors.
    /// </summary>
    internal static class ObjectConversionHelper
    {
        /// <summary>
        /// Cache for ITestElement. To ensure TestCase to TestElement mapping is unique
        /// </summary>
        private static Dictionary<Guid, ITestElement> testElementCache = new Dictionary<Guid, ITestElement>();

        /// <summary>
        /// Creates TestTools SessionStartEventArgs
        /// </summary>
        /// <param name="context">context in which data collection is being done.</param>
        /// <returns>TestTools SessionStartEventArgs</returns>
        internal static SessionStartEventArgs ToSessionStartEventArgs(DataCollectionEnvironmentContext context)
        {
            var dataCollectionContext = new DataCollectionContext(context.SessionDataCollectionContext.SessionId);
            return new SessionStartEventArgs(dataCollectionContext);
        }

        /// <summary>
        /// Creates TestTools SessionEndEventArgs
        /// </summary>
        /// <param name="context">context in which data collection is being done.</param>
        /// <returns>TestTools SessionEndEventArgs</returns>
        internal static SessionEndEventArgs ToSessionEndEventArgs(DataCollectionEnvironmentContext context)
        {
            var dataCollectionContext = new DataCollectionContext(context.SessionDataCollectionContext.SessionId);
            return new SessionEndEventArgs(dataCollectionContext);
        }

        /// <summary>
        /// Creates TestTools TestCaseStartEventArgs from TestPlatform TestCaseStartEventArgs
        /// </summary>
        /// <param name="context">context in which data collection is being done.</param>
        /// <param name="e">TestPlatform TestCaseStartEventArgs</param>
        /// <returns>TestTools TestCaseStartEventArgs</returns>
        internal static TestTools.Execution.TestCaseStartEventArgs ToTestCaseStartEventArgs(
            DataCollectionEnvironmentContext context,
            TestCaseStartEventArgs e)
        {
            var testElement = ToTestElement(e.TestCase);
            var dataCollectionContext = new DataCollectionContext(context.SessionDataCollectionContext.SessionId, testElement.ExecutionId);
            var args = new TestTools.Execution.TestCaseStartEventArgs(dataCollectionContext, testElement, null);
            return args;
        }

        /// <summary>
        /// Creates TestTools TestCaseEndEventArgs from TestPlatform TestResultEventArgs
        /// </summary>
        /// <param name="context">context in which data collection is being done.</param>
        /// <param name="testCase">Test case which is complete.</param>
        /// <param name="testOutCome">Outcome of the test case.</param>
        /// <returns>TestTools TestCaseEndEventArgs</returns>
        internal static TestTools.Execution.TestCaseEndEventArgs ToTestCaseEndEventArgs(DataCollectionEnvironmentContext context, TestCase testCase, TestOutcome testOutCome)
        {
            var testElement = ToTestElement(testCase);
            var dataCollectionContext = new DataCollectionContext(context.SessionDataCollectionContext.SessionId, testElement.ExecutionId);
            var args = new TestTools.Execution.TestCaseEndEventArgs(dataCollectionContext, testElement, null, ToOutcome(testOutCome));
            return args;
        }

        /// <summary>
        /// Converts a TestPlatform TestCase to TestTools ITestElement
        /// </summary>
        /// <param name="testCase">TestPlatform TestCase</param>
        /// <returns>TestTools ITestElement</returns>
        private static ITestElement ToTestElement(TestCase testCase)
        {
            ITestElement testElement;
            if (!testElementCache.TryGetValue(testCase.Id, out testElement))
            {
                testElement = new TestPlatformTestElement(testCase);
                testElementCache.Add(testCase.Id, testElement);
            }

            return testElement;
        }

        /// <summary>
        /// Converts TestPlatform TestOutcome to TestTools TestOutcome
        /// </summary>
        /// <param name="outcome">TestPlatform TestOutcome</param>
        /// <returns>TestTools TestOutcome</returns>
        private static TestTools.Common.TestOutcome ToOutcome(ObjectModel.TestOutcome outcome)
        {
            switch (outcome)
            {
                case TestPlatform.ObjectModel.TestOutcome.Failed:
                    return TestTools.Common.TestOutcome.Failed;

                case TestPlatform.ObjectModel.TestOutcome.Passed:
                    return TestTools.Common.TestOutcome.Passed;

                case TestPlatform.ObjectModel.TestOutcome.Skipped:
                    return TestTools.Common.TestOutcome.NotExecuted;

                case TestPlatform.ObjectModel.TestOutcome.None:
                case TestPlatform.ObjectModel.TestOutcome.NotFound:
                default:
                    return TestTools.Common.TestOutcome.NotRunnable;
            }
        }

    }
}
