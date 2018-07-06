// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using System.Collections.Generic;

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
            ValidateArgumentNotEmpty(argument);

            var diagFilePath = GetDiagFilePath(argument);
            ValidateArgumentNotEmpty(diagFilePath);

            var arguments = GetDiagArguments(argument);

            InitializeDiagLogging(diagFilePath, arguments);

            // Following patterns should match
            // "abc"
            // "abc";verbosity=xyz
            // abc;verbosity=xyz
            // Here abc and xyz can have any character including ", ;, =

            // Pattern 1:
            // Starts with quote
            // Atleast 2 quotes in string (including starting one)
            // Between first 2 quotes, there should be atleast one non-whitespace char.
            // After 2 quotes, there can be string or not.
            // If there is string after 2 quotes, it should start with ;
            // Remaining of the string expect can be empty or non empty
            // Remaining of the string needs be split using ; [remove empty entries]
            // Search among all the values and split each value using =. Each split should have exactly one =.
            // Understand verbosity key. Ignore rest key value pairs.

            // Pattern 2:
            // Doesn't start with quote
            // Entire arg should be considered as diag file path.

            //if (string.IsNullOrWhiteSpace(argument))
            //{
            //    // /diag:, /diag:  ,
            //    // Check if /diag, belong here.
            //    throw new CommandLineException(CommandLineResources.EnableDiagUsage);
            //}

            //if (argument.StartsWith('"'))
            //{
            //    int startQuoteOfFilePath = 0;
            //    int endQuoteOfFilePath = argument.IndexOf('"', startQuoteOfFilePath + 1); // TODO: does this throw error on / diag:", scenario?
            //    int diagFilePathLength = endQuoteOfFilePath - startQuoteOfFilePath - 1;
            //    if (endQuoteOfFilePath > 0 && diagFilePathLength > 0)
            //    {
            //        // /diag:" ", /diag:"a",
            //        var diagFilePath = argument.Substring(startQuoteOfFilePath + 1, endQuoteOfFilePath - startQuoteOfFilePath);
            //        // var parameters
            //    }
            //    else
            //    {
            //        // / diag:", /diag:"", /diag:"abc,
            //        throw new CommandLineException(CommandLineResources.EnableDiagUsage);
            //    }
            //}
            //else
            //{
            //    // pattern 2
            //}

            // TODO: try /diag, /diag:, /diag: , /diag:  , /diag:"",
            //if (string.IsNullOrWhiteSpace(argument) ||
            //    argument.StartsWith(@""""))
            //{
            //    throw new CommandLineException(CommandLineResources.EnableDiagUsage);
            //}

            //var diagFilePath = string.Empty;

            //if (argument.StartsWith('"'))
            //{
            //    //  TODO: /diag:"a, /diag:"abc"def", /diag:"abc"d"ef", , /diag:"abc"def, /diag:"abc"   , /diag:"  "abc;
            //    var ArgumentSeperator = new char[] { '"' };
            //    var argumentParts = argument.Split(ArgumentSeperator, StringSplitOptions.None);
            //    if (argumentParts.Length > 0)
            //    {
            //        diagFilePath = argumentParts[0];
            //    }
            //    else
            //    {
            //        //  TODO: /diag:", /diag:" , /diag:"  ",
            //        throw new CommandLineException(CommandLineResources.EnableDiagUsage);
            //    }
            //}

            //  TODO: /diag:a, /diag:a"

            if (string.IsNullOrWhiteSpace(Path.GetExtension(argument)))
            {
                // Throwing error if the argument is just path and not a file
                throw new CommandLineException(CommandLineResources.EnableDiagUsage);
            }

            // Create the base directory for logging if doesn't exist. Directory could be empty if just a
            // filename is provided. E.g. log.txt
            var logDirectory = Path.GetDirectoryName(argument);
            if (!string.IsNullOrEmpty(logDirectory) && !this.fileHelper.DirectoryExists(logDirectory))
            {
                this.fileHelper.CreateDirectory(logDirectory);
            }

            // Find full path and send this to testhost so that vstest and testhost create logs at same location.
            argument = Path.GetFullPath(argument);

            // Catch exception(UnauthorizedAccessException, PathTooLongException...) if there is any at time of initialization.
            if (!EqtTrace.InitializeVerboseTrace(argument))
            {
                if (!string.IsNullOrEmpty(EqtTrace.ErrorOnInitialization))
                    ConsoleOutput.Instance.Warning(false, EqtTrace.ErrorOnInitialization);
            }
        }

        private void InitializeDiagLogging(string diagFilePath, object arguments)
        {
            throw new NotImplementedException();
        }

        private object GetDiagArguments(string argument)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Validates that argument is not empty.
        /// Throws CommandLineException in case argument is empty.
        /// </summary>
        /// <param name="argument">Diag argument.</param>
        private void ValidateArgumentNotEmpty(string argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new CommandLineException(CommandLineResources.EnableDiagUsage);
            }
        }

        /// <summary>
        /// Gets diag file path.
        /// </summary>
        /// <param name="argument">Argument.</param>
        /// <returns>Diag file path.</returns>
        private string GetDiagFilePath(string argument)
        {
            // If quotes are present in argument, value between first two quotes is considered as diag file path.
            bool startsWithQuote = argument.StartsWith('"');
            if (startsWithQuote)
            {
                var firstQuoteIndex = 0;
                var secondQuoteIndex = argument.IndexOf('"', firstQuoteIndex + 1);
                return argument.Substring(firstQuoteIndex + 1, secondQuoteIndex - firstQuoteIndex);
            }

            // If no quotes are present, entire argument is considered as diag file path.
            return argument;
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

        #endregion

        public static bool TryParseDiagArgument(string argument, out string diagFilePath, out Dictionary<string, string> parameters)
        {
            diagFilePath = null;
            parameters = null;

            var parseSucceeded = true;
            var ArgumentSeperator = new char[] { ';' };
            var NameValueSeperator = new char[] { '=' };

            var argumentParts = argument.Split(ArgumentSeperator, StringSplitOptions.RemoveEmptyEntries);

            if (argumentParts.Length > 0 && !argumentParts[0].Contains("="))
            {
                diagFilePath = argumentParts[0];

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
