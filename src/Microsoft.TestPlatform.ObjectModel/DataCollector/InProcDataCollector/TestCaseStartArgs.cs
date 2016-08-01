// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector
{
    /// <summary>
    /// The test case start args.
    /// </summary>
    public class TestCaseStartArgs : InProcDataCollectionArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestCaseStartArgs"/> class.
        /// </summary>
        /// <param name="testCase">
        /// The test case.
        /// </param>
        public TestCaseStartArgs(TestCase testCase)
        {
            this.TestCase = testCase;
        }

        /// <summary>
        /// Gets the test case.
        /// </summary>
        public TestCase TestCase { get; private set; }
    }
}
