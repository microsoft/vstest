// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities
{
    using System;
    using System.Collections.Generic;

    using Common.Logging;

    using ObjectModel;
    using ObjectModel.Logging;

    internal class LoggerUtilities
    {
        internal static void RaiseTestRunError(TestLoggerManager loggerManager, TestRunResultAggregator testRunResultAggregator, Exception exception)
        {
            // testRunResultAggregator can be null, if error is being raised in discovery context.
            testRunResultAggregator?.MarkTestRunFailed();

            TestRunMessageEventArgs errorMessage = new TestRunMessageEventArgs(TestMessageLevel.Error, exception.Message);
            loggerManager.SendTestRunMessage(errorMessage);

            // Send inner exception only when its message is different to avoid duplicate.
            if (exception is TestPlatformException && exception.InnerException != null && string.Compare(exception.Message, exception.InnerException.Message, StringComparison.CurrentCultureIgnoreCase) != 0)
            {
                errorMessage = new TestRunMessageEventArgs(TestMessageLevel.Error, exception.InnerException.Message);
                loggerManager.SendTestRunMessage(errorMessage);
            }
        }

        internal static void RaiseTestRunWarning(TestLoggerManager loggerManager, TestRunResultAggregator testRunResultAggregator, string warningMessage)
        {
            TestRunMessageEventArgs testRunMessage = new TestRunMessageEventArgs(TestMessageLevel.Warning, warningMessage);
            loggerManager.SendTestRunMessage(testRunMessage);
        }
    }
}
