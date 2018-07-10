// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities
{
    using System;
    using System.Collections.Generic;

    internal class ArgumemtProcessorUtilities
    {
        public static readonly char[] SemiColonArgumentSeperator = { ';' };
        public static readonly char[] EqualNameValueSeperator = { '=' };

        /// <summary>
        /// Get argument list from raw argument usign argument separator.
        /// </summary>
        /// <param name="rawArgument">Raw argument.</param>
        /// <param name="argumentSeparator">Argument separator.</param>
        /// <param name="exceptionMessage">Exception Message.</param>
        /// <returns>Argument list.</returns>
        public static string[] GetArgumentList(string rawArgument, char[] argumentSeparator, string exceptionMessage)
        {
            var argumentList = rawArgument?.Split(argumentSeparator, StringSplitOptions.RemoveEmptyEntries);

            // Throw error in case of invalid argument.
            if (argumentList == null || argumentList.Length <= 0)
            {
                throw new CommandLineException(exceptionMessage);
            }

            return argumentList;
        }

        /// <summary>
        /// Get argument parameters.
        /// </summary>
        /// <param name="parameterArgs">Parameter args.</param>
        /// <returns>Parameters dictionary.</returns>
        public static Dictionary<string, string> GetArgumentParameters(IEnumerable<string> parameterArgs, char[] nameValueSeparator, string exceptionMessage)
        {
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Get parameters from parameterNameValuePairs.
            // Throw error in case of invalid name value pairs.
            foreach (string parameterArg in parameterArgs)
            {
                var nameValuePair = parameterArg?.Split(nameValueSeparator, StringSplitOptions.RemoveEmptyEntries);
                if (nameValuePair.Length == 2)
                {
                    parameters[nameValuePair[0]] = nameValuePair[1];
                }
                else
                {
                    throw new CommandLineException(exceptionMessage);
                }
            }

            return parameters;
        }
    }
}
