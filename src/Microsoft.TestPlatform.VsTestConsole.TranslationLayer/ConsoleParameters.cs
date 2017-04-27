// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using System;
    using System.IO;

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
                if (!fileHelper.DirectoryExists(Path.GetDirectoryName(value)))
                {
                    throw new ArgumentException("LogFilePath must point to a valid directory for logging!");
                }

                // Ensure path is double quoted. if path has white space then it can create problem.
                this.logFilePath = "\"" + value + "\"";
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
