// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;

    /// <summary>
    /// Class for unit test result.
    /// </summary>
    internal class UnitTestResult: TestResultAggregation
    {
        public UnitTestResult(
            string testName,
            string computerName, 
            Guid runId, 
            Guid executionId,
            Guid parentExecutionId,
            ITestElement test, 
            TestOutcome outcome) : base(testName, computerName, runId, executionId, parentExecutionId, test, outcome) { }
    }
}
