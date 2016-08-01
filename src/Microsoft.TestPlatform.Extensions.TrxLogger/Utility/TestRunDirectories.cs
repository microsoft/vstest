// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.Utility
{
    using System;

    /// <summary>
    /// Class to deal with directories.
    /// </summary>
    internal sealed class TestRunDirectories
    {
        /// <summary>
        /// Computes the test results directory, relative to the root results directory (whatever that may be)
        /// </summary>
        /// <param name="testExecutionId">The test's execution ID</param>
        /// <returns>
        /// The test results directory (&lt;testExecutionId&gt;), under which test-specific result files should be stored
        /// </returns>
        public static string GetRelativeTestResultsDirectory(Guid testExecutionId)
        {
            return testExecutionId.ToString();
        }
    }
}
