// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector
{
    /// <summary>
    /// The test case end args.
    /// </summary>
    public class TestCaseEndArgs : InProcDataCollectionArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestCaseEndArgs"/> class.
        /// </summary>
        /// <param name="testCase">
        /// The test case.
        /// </param>
        /// <param name="outcome">
        /// The outcome.
        /// </param>
        public TestCaseEndArgs(TestCase testCase, TestOutcome outcome)
        {
            this.TestCase = testCase;
            this.TestOutcome = outcome;
        }

        /// <summary>
        /// Gets the test case.
        /// </summary>
        public TestCase TestCase { get; private set; }

        /// <summary>
        /// Gets the outcome.
        /// </summary>
        public TestOutcome TestOutcome { get; private set; }
    }
}
