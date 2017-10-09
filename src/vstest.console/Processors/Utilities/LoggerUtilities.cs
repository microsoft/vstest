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
            if (null != testRunResultAggregator)
            {
                testRunResultAggregator.MarkTestRunFailed();
            }

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
            // testRunResultAggregator can be null, if error is being raised in discovery context.
            if (null != testRunResultAggregator)
            {
                testRunResultAggregator.MarkTestRunFailed();
            }

            TestRunMessageEventArgs testRunMessage = new TestRunMessageEventArgs(TestMessageLevel.Warning, warningMessage);
            loggerManager.SendTestRunMessage(testRunMessage);
        }

        /// <summary>
        /// Parses the parameters passed as name values pairs along with the logger argument.
        /// </summary>
        /// <param name="argument">Logger argument</param>
        /// <param name="loggerIdentifier">Receives logger Uri or friendly name.</param>
        /// <param name="paramters">Receives parse name value pairs.</param>
        /// <returns>True is successful, false otherwise.</returns>
        public static bool TryParseLoggerArgument(string argument, out string loggerIdentifier, out Dictionary<string, string> parameters)
        {
            loggerIdentifier = null;
            parameters = null;

            var parseSucceeded = true;
            char[] ArgumentSeperator = new char[] { ';' };
            char[] NameValueSeperator = new char[] { '=' };

            var argumentParts = argument.Split(ArgumentSeperator, StringSplitOptions.RemoveEmptyEntries);

            if (argumentParts.Length > 0 && !argumentParts[0].Contains("="))
            {
                loggerIdentifier = argumentParts[0];

                if (argumentParts.Length > 1)
                {
                    parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (int index = 1; index < argumentParts.Length; ++index)
                    {
                        string[] nameValuePair = argumentParts[index].Split(NameValueSeperator, StringSplitOptions.RemoveEmptyEntries);
                        if (nameValuePair.Length == 2)
                        {
                            parameters[nameValuePair[0]] = nameValuePair[1];
                        }
                        else
                        {
                            parseSucceeded = false;
                            break;
                        }
                    }
                }
            }
            else
            {
                parseSucceeded = false;
            }

            return parseSucceeded;
        }
    }
}
