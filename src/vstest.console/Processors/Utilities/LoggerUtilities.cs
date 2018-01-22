// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using ObjectModel;
    using ObjectModel.Logging;

    internal class LoggerUtilities
    {
        /// <summary>
        /// Parses the parameters passed as name values pairs along with the logger argument.
        /// </summary>
        /// <param name="argument">Logger argument</param>
        /// <param name="loggerIdentifier">Receives logger Uri or friendly name.</param>
        /// <param name="parameters">Receives parse name value pairs.</param>
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

        /// <summary>
        /// Gets existing logger index.
        /// </summary>
        /// <param name="friendlyName"></param>
        /// <param name="uri"></param>
        /// <param name="loggerSettingsList"></param>
        /// <returns></returns>
        public static int GetExistingLoggerIndex(string friendlyName, Uri uri, Collection<LoggerSettings> loggerSettingsList)
        {
            var oldLoggerIndex = -1;

            for (int i = 0; i < loggerSettingsList.Count; i++)
            {
                var logger = loggerSettingsList[i];

                if (logger.FriendlyName != null &&
                    friendlyName != null &&
                    logger.FriendlyName.Equals(friendlyName, StringComparison.OrdinalIgnoreCase))
                {
                    oldLoggerIndex = i;
                    break;
                }

                if (logger.Uri?.ToString() != null &&
                    uri?.ToString() != null &&
                    logger.Uri.ToString().Equals(uri.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    oldLoggerIndex = i;
                    break;
                }
            }

            return oldLoggerIndex;
        }
    }
}
