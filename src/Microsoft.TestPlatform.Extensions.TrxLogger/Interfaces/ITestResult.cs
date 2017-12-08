// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    internal interface ITestResult
    {
        DateTime StartTime { get; set; }
        DateTime EndTime { get; set; }
        TimeSpan Duration { get; set; }
        string ComputerName { get; }
        TestOutcome Outcome { get; set; }
        TestResultId Id { get; }
        string ErrorMessage { get; set; }
        string ErrorStackTrace { get; set; }
        string[] TextMessages { get; set; }
        string StdOut { get; set; }
        string StdErr { get; set; }
        string DebugTrace { get; set; }
        string TestResultsDirectory { get; }
        string RelativeTestResultsDirectory { get; }
        int DataRowInfo { get; set; }
        string ResultType { get; set; }
    }
}
