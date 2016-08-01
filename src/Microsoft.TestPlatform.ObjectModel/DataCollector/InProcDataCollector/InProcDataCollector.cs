// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.InProcDataCollector
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;

    /// <summary>
    /// Listener interface for external exe from test host
    /// </summary>
    public interface InProcDataCollection
    {
        /// <summary>
        /// Called when test session starts
        /// </summary>
        /// <param name="testSessionStartArgs">
        /// The test Session Start Args.
        /// </param>
        void TestSessionStart(TestSessionStartArgs testSessionStartArgs);

        /// <summary>
        /// Called when test case starts
        /// </summary>
        /// <param name="testCaseStartArgs">
        /// Test Case start args
        /// </param>
        void TestCaseStart(TestCaseStartArgs testCaseStartArgs);

        /// <summary>
        /// Called when test case end
        /// </summary>
        /// <param name="testCaseEndArgs">
        /// The test Case End Args.
        /// </param>
        void TestCaseEnd(TestCaseEndArgs testCaseEndArgs);

        /// <summary>
        /// Called when test session end
        /// </summary>
        /// <param name="testSessionEndArgs">
        /// The test Session End Args.
        /// </param>
        void TestSessionEnd(TestSessionEndArgs testSessionEndArgs);
    }
}
