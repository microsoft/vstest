// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    internal class EnableDiagArgumentProcessor : IArgumentProcessor
    {
        /// <summary>
        /// The name of the command line argument that the ListTestsArgumentExecutor handles.
        /// </summary>
        public const string CommandName = "/Diag";

        private readonly IFileHelper fileHelper;

        private Lazy<IArgumentProcessorCapabilities> metadata;

        private Lazy<IArgumentExecutor> executor;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnableDiagArgumentProcessor"/> class.
        /// </summary>
        public EnableDiagArgumentProcessor() : this(new FileHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnableDiagArgumentProcessor"/> class.
        /// </summary>
        /// <param name="fileHelper">A file helper instance.</param>
        protected EnableDiagArgumentProcessor(IFileHelper fileHelper)
        {
            this.fileHelper = fileHelper;
        }

        public Lazy<IArgumentProcessorCapabilities> Metadata
        {
            get
            {
                if (this.metadata == null)
                {
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new EnableDiagArgumentProcessorCapabilities());
                }

                return this.metadata;
            }
        }

        /// <summary>
        /// Gets or sets the executor.
        /// </summary>
        public Lazy<IArgumentExecutor> Executor
        {
            get
            {
                if (this.executor == null)
                {
                    this.executor = new Lazy<IArgumentExecutor>(() => new EnableDiagArgumentExecutor(this.fileHelper));
                }

                return this.executor;
            }

            set
            {
                this.executor = value;
            }
        }
    }

    /// <summary>
    /// The argument capabilities.
    /// </summary>
    internal class EnableDiagArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => EnableDiagArgumentProcessor.CommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Diag;

        public override string HelpContentResourceName => CommandLineResources.EnableDiagUsage;

        public override HelpContentPriority HelpPriority => HelpContentPriority.EnableDiagArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// The argument executor.
    /// </summary>
    internal class EnableDiagArgumentExecutor : IArgumentExecutor
    {
        private readonly IFileHelper fileHelper;

        /// <summary>
        /// Parameter for trace level
        /// </summary>
        public const string TraceLevelParam = "tracelevel";

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="fileHelper">The file helper.</param>
        public EnableDiagArgumentExecutor(IFileHelper fileHelper)
        {
            this.fileHelper = fileHelper;
        }

        #endregion

        #region IArgumentExecutor

        /// <summary>
        /// Initializes with the argument that was provided with the command.
        /// </summary>
        /// <param name="argument">Argument that was provided with the command.</param>
        public void Initialize(string argument)
        {
            // Throw error if argument is null or empty.
            if (string.IsNullOrWhiteSpace(argument))
            {
                HandleInvalidDiagArgument();
            }

            // Get diag argument list.
            var diagArgumentList = GetDiagArgumentList(argument);

            // Get diag file path.
            var diagFilePathArg = diagArgumentList[0];
            var diagFilePath = GetDiagFilePath(diagFilePathArg);

            // Get diag parameters.
            var diagParameterArgs = diagArgumentList.Skip(1);
            var diagParameters = GetDiagParameters(diagParameterArgs);

            // Initialize diag logging.
            InitializeDiagLogging(diagFilePath, diagParameters);
        }

        /// <summary>
        /// Executes the argument processor.
        /// </summary>
        /// <returns>The <see cref="ArgumentProcessorResult"/>.</returns>
        public ArgumentProcessorResult Execute()
        {
            // Nothing to do since we updated the parameter during initialize parameter
            return ArgumentProcessorResult.Success;
        }

        /// <summary>
        /// Get diag parameters.
        /// </summary>
        /// <param name="diagParameterArgs">Diag parameter args.</param>
        /// <returns>Diag parameters dictionary.</returns>
        private Dictionary<string, string> GetDiagParameters(IEnumerable<string> diagParameterArgs)
        {
            var nameValueSeperator = new char[] { '=' };
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Get parameters from parameterNameValuePairs.
            // Throw error in case of invalid name value pairs.
            foreach (string diagParameterArg in diagParameterArgs)
            {
                var nameValuePair = diagParameterArg?.Split(nameValueSeperator, StringSplitOptions.RemoveEmptyEntries);
                if (nameValuePair.Length == 2)
                {
                    parameters[nameValuePair[0]] = nameValuePair[1];
                }
                else
                {
                    HandleInvalidDiagArgument();
                }
            }

            return parameters;
        }

        /// <summary>
        /// Get diag argument list.
        /// </summary>
        /// <param name="argument">Argument.</param>
        /// <returns>Diag argument list.</returns>
        private string[] GetDiagArgumentList(string argument)
        {
            var argumentSeperator = new char[] { ';' };
            var diagArgumentList = argument?.Split(argumentSeperator, StringSplitOptions.RemoveEmptyEntries);

            // Handle invalid diag argument.
            if (diagArgumentList == null || diagArgumentList.Length <= 0)
            {
                HandleInvalidDiagArgument();
            }

            return diagArgumentList;
        }

        /// <summary>
        /// Initialize diag loggin.
        /// </summary>
        /// <param name="diagFilePath">Diag file path.</param>
        /// <param name="diagParameters">Diag parameters</param>
        private void InitializeDiagLogging(string diagFilePath, Dictionary<string, string> diagParameters)
        {
            // Get trace level from diag parameters.
            var traceLevel = GetDiagTraceLevel(diagParameters);

            // Initialize trace.
            // Trace initialized is false in case of any exception at time of initialization like Catch exception(UnauthorizedAccessException, PathTooLongException...)
            var traceInitialized = EqtTrace.InitializeTrace(diagFilePath, traceLevel);

            // Show console warning in case trace is not initialized.
            if (!traceInitialized && !string.IsNullOrEmpty(EqtTrace.ErrorOnInitialization))
            {
                ConsoleOutput.Instance.Warning(false, EqtTrace.ErrorOnInitialization);
            }
        }

        /// <summary>
        /// Gets diag trace level.
        /// </summary>
        /// <param name="diagParameters">Diag parameters.</param>
        /// <returns>Diag trace level.</returns>
        private PlatformTraceLevel GetDiagTraceLevel(Dictionary<string, string> diagParameters)
        {
            // If diag parameters is null, set value of trace level as verbose.
            if (diagParameters == null)
            {
                return PlatformTraceLevel.Verbose;
            }

            // Get trace level from diag parameters.
            var traceLevelExists = diagParameters.TryGetValue(TraceLevelParam, out string traceLevelStr);
            if (traceLevelExists && Enum.TryParse(traceLevelStr, true, out PlatformTraceLevel traceLevel))
            {
                return traceLevel;
            }

            // Default value of diag trace level is verbose.
            return PlatformTraceLevel.Verbose;
        }

        /// <summary>
        /// Throws an exception indicating that the diag argument is invalid.
        /// </summary>
        private static void HandleInvalidDiagArgument()
        {
            throw new CommandLineException(CommandLineResources.EnableDiagUsage);
        }

        /// <summary>
        /// Gets diag file path.
        /// </summary>
        /// <param name="diagFilePathArgument">Diag file path argument.</param>
        /// <returns>Diag file path.</returns>
        private string GetDiagFilePath(string diagFilePathArgument)
        {
            // Throw error in case diag file path is not a valid file path
            var fileExtension = Path.GetExtension(diagFilePathArgument);
            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                HandleInvalidDiagArgument();
            }

            // Create base directory for diag file path (if doesn't exist)
            CreateDirectoryIfNotExists(diagFilePathArgument);

            // return full diag file path. (This is done so that vstest and testhost create logs at same location.)
            return Path.GetFullPath(diagFilePathArgument);
        }

        /// <summary>
        /// Create directory if not exists.
        /// </summary>
        /// <param name="filePath">File path.</param>
        private void CreateDirectoryIfNotExists(string filePath)
        {
            // Create the base directory of file path if doesn't exist.
            // Directory could be empty if just a filename is provided. E.g. log.txt
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !this.fileHelper.DirectoryExists(directory))
            {
                this.fileHelper.CreateDirectory(directory);
            }
        }

        #endregion
    }
}
