// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer
{
    using System;
#if NETFRAMEWORK
    using System.Collections.Generic;
#endif
    using System.Diagnostics;
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    /// <summary>
    /// Class which defines additional specifiable parameters for vstest.console.exe
    /// </summary>
    public class ConsoleParameters
    {
        internal static readonly ConsoleParameters Default = new ConsoleParameters();

        private string logFilePath = null;
        private IFileHelper fileHelper;

        /// <summary>
        /// Create instance of <see cref="ConsoleParameters"/>
        /// </summary>
        public ConsoleParameters() : this(new FileHelper())
        { }

        /// <summary>
        /// Create instance of <see cref="ConsoleParameters"/>
        /// </summary>
        /// <param name="fileHelper"> Object of type <see cref="IFileHelper"/></param>
        public ConsoleParameters(IFileHelper fileHelper)
        {
            this.fileHelper = fileHelper;
        }

#if NETFRAMEWORK

        /// <summary>
        /// TODO: Remove the #if when project is targeted to netstandard2.0
        /// Environment variables to be set for the process
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; set; }

#endif

        /// <summary>
        /// Trace level for logs.
        /// </summary>
        public TraceLevel TraceLevel { get; set; } = TraceLevel.Verbose;

        /// <summary>
        /// Full path for the log file
        /// </summary>
        public string LogFilePath
        {
            get
            {
                return logFilePath;
            }

            set
            {
                ValidateArg.NotNullOrEmpty(value, "LogFilePath");
                var directoryPath = Path.GetDirectoryName(value);
                if (!string.IsNullOrEmpty(directoryPath) && !fileHelper.DirectoryExists(Path.GetDirectoryName(value)))
                {
                    throw new ArgumentException("LogFilePath must point to a valid directory for logging!");
                }

                // Ensure path is double quoted. if path has white space then it can create problem.
                this.logFilePath = value.AddDoubleQuote();
            }
        }

        /// <summary>
        /// Port Number for communication
        /// vstest.console will need this port number to communicate with this component - translation layer
        /// Currently Internal as we are not intentionally exposing this to consumers of translation layer
        /// </summary>
        internal int PortNumber { get; set; }

        /// <summary>
        /// Parent Process ID of the process whose lifetime should dictate the life time of vstest.console.exe
        /// vstest.console will need this process ID to know when the process exits.
        /// If parent process dies/crashes without invoking EndSession, vstest.console should exit immediately
        /// Currently Internal as we are not intentionally exposing this to consumers of translation layer
        /// </summary>
        internal int ParentProcessId { get; set; }
    }
}
