// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.VisualStudio.TestPlatform.Client
{
    using System.Collections.Generic;
    public class LoggerList
    {
        private static LoggerList loggerList;
        private List<LoggerInfo> loggers = new List<LoggerInfo>();

        protected LoggerList()
        {
        }

        public static LoggerList Instance
        {
            get
            {
                if (loggerList == null)
                {
                    loggerList = new LoggerList();
                }
                return loggerList;
            }

            protected set
            {
                loggerList = value;
            }
        }

        public IEnumerable<LoggerInfo> Loggers
        {
            get
            {
                return this.loggers;
            }
        }

        /// <summary>
        /// Update the list of logger with their identifier and parameter.
        /// </summary>
        /// <param name="arument">Argument pass by user</param>
        /// <param name="loggerIdentifier">Identifier of the logger</param>
        /// <param name="parameters">parameter for logger</param>
        public void AddLogger(string arument, string loggerIdentifier, Dictionary<string, string> parameters)
        {
            this.loggers.Add(new LoggerInfo(arument, loggerIdentifier, parameters));
        }
    }

    public class LoggerInfo
    {
        public string arument;
        public string loggerIdentifier;
        public Dictionary<string, string> parameters = new Dictionary<string, string>();

        public LoggerInfo(string arument, string loggerIdentifier, Dictionary<string, string> parameters)
        {
            this.arument = arument;
            this.loggerIdentifier = loggerIdentifier;
            this.parameters = parameters;
        }
    }
}
