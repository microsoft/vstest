﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

#nullable disable

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer;

/// <summary>
/// Class which defines additional specifiable parameters for vstest.console.exe
/// </summary>
public class ConsoleParameters
{
    internal static readonly ConsoleParameters Default = new();

    private string _logFilePath;
    private readonly IFileHelper _fileHelper;

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
        _fileHelper = fileHelper;
    }

    /// <summary>
    /// Environment variables to be set for the process. This will add the specified entries to the environment variables
    /// inherited from the current process. If you wish to provide a full set of environment variables yourself set <see cref="ClearEnvironmentVariables"/> to true.
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Clears all environment variables that would be inherited from machine, user and process
    /// and only sets the entries you provided in <see cref="EnvironmentVariables"/>.
    /// This allows you to provide an arbitrary set of environment variables, for example when you
    /// want to skip environment variables that are set in the IDE that starts vstest.console.
    /// When setting this to true you are responsible for providing a full set of envirionment variables
    /// that allow the process to start.
    /// You most likely want to use <see cref="System.Environment.GetEnvironmentVariables(System.EnvironmentVariableTarget)"/> and combine
    /// <see cref="System.EnvironmentVariableTarget.Machine"/> and <see cref="System.EnvironmentVariableTarget.User"/> responses.
    /// </summary>
    public bool ClearEnvironmentVariables { get; set; }

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
            return _logFilePath;
        }

        set
        {
            ValidateArg.NotNullOrEmpty(value, "LogFilePath");
            var directoryPath = Path.GetDirectoryName(value);
            if (!string.IsNullOrEmpty(directoryPath) && !_fileHelper.DirectoryExists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Ensure path is double quoted. if path has white space then it can create problem.
            _logFilePath = value.AddDoubleQuote();
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
