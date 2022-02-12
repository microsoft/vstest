// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel;

using System;

using Utility;

/// <summary>
/// Class for unit test result.
/// </summary>
internal class UnitTestResult : TestResultAggregation
{
    public UnitTestResult(
        Guid runId,
        Guid testId,
        Guid executionId,
        Guid parentExecutionId,
        string resultName,
        string computerName,
        TestOutcome outcome,
        TestType testType,
        TestListCategoryId testCategoryId,
        TrxFileHelper trxFileHelper
    ) : base(runId, testId, executionId, parentExecutionId, resultName, computerName, outcome, testType, testCategoryId, trxFileHelper) { }
}
