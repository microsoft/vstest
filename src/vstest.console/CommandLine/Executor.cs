// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// General Flow:
// Create a command processor for each argument.
//   If there is no matching command processor for an argument, output error, display help and exit.
//   If throws during creation, output error and exit.
// If the help command processor has been requested, execute the help processor and exit.
// Order the command processors by priority.
// Allow command processors to validate against other command processors which are present.
//   If throws during validation, output error and exit.
// Process each command processor.
//   If throws during validation, output error and exit.
//   If the default (RunTests) command processor has no test containers output an error and exit
//   If the default (RunTests) command processor has no tests to run output an error and exit

// Commands metadata:
//  *Command line argument.
//   Priority.
//   Help output.
//   Required
//   Single or multiple

namespace Microsoft.VisualStudio.TestPlatform.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Internal;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    /// Performs the execution based on the arguments provided.
    /// </summary>
    internal class Executor
    {
        private ITestPlatformEventSource testPlatformEventSource;
        private bool showHelp;

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Executor(IOutput output) : this(output, TestPlatformEventSource.Instance)
        {
        }

        internal Executor(IOutput output, ITestPlatformEventSource testPlatformEventSource)
        {
            this.Output = output;
            this.testPlatformEventSource = testPlatformEventSource;
            this.showHelp = true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Instance to use for sending output.
        /// </summary>
        private IOutput Output { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Performs the execution based on the arguments provided.
        /// </summary>
        /// <param name="args">
        /// Arguments provided to perform execution with.
        /// </param>
        /// <returns>
        /// Exit Codes - Zero (for successful command execution), One (for bad command) 
        /// </returns>
        internal int Execute(params string[] args)
        {
            this.testPlatformEventSource.VsTestConsoleStart();

            // If User specifies --nologo via dotnet, do not print splat screen
            if (args != null && args.Length !=0 && args.Contains("--nologo"))
            {
                // Sanitizing this list, as I don't think we should write Argument processor for this.
                args = args.Where(val => val != "--nologo").ToArray();
            }
            else
            {
                var isDiag = args != null && args.Any(arg => arg.StartsWith("--diag", StringComparison.OrdinalIgnoreCase));
                this.PrintSplashScreen(isDiag);
            }

            int exitCode = 0;

            // If we have no arguments, set exit code to 1, add a message, and include the help processor in the args.
            if (args == null || args.Length == 0 || args.Any(string.IsNullOrWhiteSpace))
            {
                this.Output.Error(true, CommandLineResources.NoArgumentsProvided);
                args = new string[] { HelpArgumentProcessor.CommandName };
                exitCode = 1;
            }

            // Flatten arguments and process response files.
            string[] flattenedArguments;
            exitCode |= this.FlattenArguments(args, out flattenedArguments);

            // Get the argument processors for the arguments.
            List<IArgumentProcessor> argumentProcessors;
            exitCode |= this.GetArgumentProcessors(flattenedArguments, out argumentProcessors);

            // Verify that the arguments are valid.
            exitCode |= this.IdentifyDuplicateArguments(argumentProcessors);

            // Quick exit for syntax error
            if (exitCode == 1
                && argumentProcessors.All(
                    processor => processor.Metadata.Value.CommandName != HelpArgumentProcessor.CommandName))
            {
                this.testPlatformEventSource.VsTestConsoleStop();
                return exitCode;
            }

            // Execute all argument processors
            foreach (var processor in argumentProcessors)
            {
                if (!this.ExecuteArgumentProcessor(processor, ref exitCode))
                {
                    break;
                }
            }

            // Use the test run result aggregator to update the exit code.
            exitCode |= (TestRunResultAggregator.Instance.Outcome == TestOutcome.Passed) ? 0 : 1;

            EqtTrace.Verbose("Executor.Execute: Exiting with exit code of {0}", exitCode);

            this.testPlatformEventSource.VsTestConsoleStop();

            this.testPlatformEventSource.MetricsDisposeStart();

            // Disposing Metrics Publisher when VsTestConsole ends
            TestRequestManager.Instance.Dispose();

            this.testPlatformEventSource.MetricsDisposeStop();
            return exitCode;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Get the list of argument processors for the arguments.
        /// </summary>
        /// <param name="args">Arguments provided to perform execution with.</param>
        /// <param name="processors">List of argument processors for the arguments.</param>
        /// <returns>0 if all of the processors were created successfully and 1 otherwise.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "processorInstance", Justification = "Done on purpose to force the instances to be created")]
        private int GetArgumentProcessors(string[] args, out List<IArgumentProcessor> processors)
        {
            processors = new List<IArgumentProcessor>();
            int result = 0;
            var processorFactory = ArgumentProcessorFactory.Create();
            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];
                // If argument is '--', following arguments are key=value pairs for run settings.
                if (arg.Equals("--"))
                {
                    var cliRunSettingsProcessor = processorFactory.CreateArgumentProcessor(arg, args.Skip(index + 1).ToArray());
                    processors.Add(cliRunSettingsProcessor);
                    break;
                }

                var processor = processorFactory.CreateArgumentProcessor(arg);

                if (processor != null)
                {
                    processors.Add(processor);
                }
                else
                {
                    // No known processor was found, report an error and continue
                    this.Output.Error(false, string.Format(CultureInfo.CurrentCulture, CommandLineResources.NoArgumentProcessorFound, arg));

                    // Add the help processor
                    if (result == 0)
                    {
                        result = 1;
                        processors.Add(processorFactory.CreateArgumentProcessor(HelpArgumentProcessor.CommandName));
                    }
                }
            }

            // Add the internal argument processors that should always be executed.
            // Examples: processors to enable loggers that are statically configured, and to start logging,
            // should always be executed.
            var processorsToAlwaysExecute = processorFactory.GetArgumentProcessorsToAlwaysExecute();
            processors.AddRange(processorsToAlwaysExecute);

            // Initialize Runsettings with defaults
            RunSettingsManager.Instance.AddDefaultRunSettings();

            // Ensure we have an action argument.
            this.EnsureActionArgumentIsPresent(processors, processorFactory);

            // Instantiate and initialize the processors in priority order.
            processors.Sort((p1, p2) => Comparer<ArgumentProcessorPriority>.Default.Compare(p1.Metadata.Value.Priority, p2.Metadata.Value.Priority));
            foreach (var processor in processors)
            {
                IArgumentExecutor executorInstance;
                try
                {
                    // Ensure the instance is created.  Note that the Lazy not only instantiates
                    // the argument processor, but also initializes it.
                    executorInstance = processor.Executor.Value;
                }
                catch (Exception ex)
                {
                    if (ex is CommandLineException || ex is TestPlatformException || ex is SettingsException)
                    {
                        this.Output.Error(false, ex.Message);
                        result = 1;
                        this.showHelp = false;
                    }
                    else if(ex is TestSourceException)
                    {
                        this.Output.Error(false, ex.Message);
                        result = 1;
                        this.showHelp = false;
                        break;
                    }
                    else
                    {
                        // Let it throw - User must see crash and report it with stack trace!
                        // No need for recoverability as user will start a new vstest.console anyway
                        throw;
                    }
                }
            }

            // If some argument was invalid, add help argument processor in beginning(i.e. at highest priority) 
            if (result == 1 && this.showHelp && processors.First<IArgumentProcessor>().Metadata.Value.CommandName != HelpArgumentProcessor.CommandName)
            {
                processors.Insert(0, processorFactory.CreateArgumentProcessor(HelpArgumentProcessor.CommandName));
            }
            return result;
        }

        /// <summary>
        /// Verify that the arguments are valid.
        /// </summary>
        /// <param name="argumentProcessors">Processors to verify against.</param>
        /// <returns>0 if successful and 1 otherwise.</returns>
        private int IdentifyDuplicateArguments(IEnumerable<IArgumentProcessor> argumentProcessors)
        {
            int result = 0;

            // Used to keep track of commands that are only allowed to show up once.  The first time it is seen
            // an entry for the command will be added to the dictionary and the value will be set to 1.  If we
            // see the command again and the value is 1 (meaning this is the second time we have seen the command),
            // we will output an error and increment the count.  This ensures that the error message will only be
            // displayed once even if the user does something like /ListDiscoverers /ListDiscoverers /ListDiscoverers.
            var commandSeenCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Check each processor.
            foreach (var processor in argumentProcessors)
            {
                if (!processor.Metadata.Value.AllowMultiple)
                {
                    int count;
                    if (!commandSeenCount.TryGetValue(processor.Metadata.Value.CommandName, out count))
                    {
                        commandSeenCount.Add(processor.Metadata.Value.CommandName, 1);
                    }
                    else if (count == 1)
                    {
                        result = 1;

                        // Update the count so we do not print the error out for this argument multiple times.
                        commandSeenCount[processor.Metadata.Value.CommandName] = ++count;
                        this.Output.Error(false, string.Format(CultureInfo.CurrentCulture, CommandLineResources.DuplicateArgumentError, processor.Metadata.Value.CommandName));
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Ensures that an action argument is present and if one is not, then the default action argument is added.
        /// </summary>
        /// <param name="argumentProcessors">The arguments that are being processed.</param>
        /// <param name="processorFactory">A factory for creating argument processors.</param>
        private void EnsureActionArgumentIsPresent(List<IArgumentProcessor> argumentProcessors, ArgumentProcessorFactory processorFactory)
        {
            Contract.Requires(argumentProcessors != null);
            Contract.Requires(processorFactory != null);

            // Determine if any of the argument processors are actions.
            var isActionIncluded = argumentProcessors.Any((processor) => processor.Metadata.Value.IsAction);

            // If no action arguments have been provided, then add the default action argument.
            if (!isActionIncluded)
            {
                argumentProcessors.Add(processorFactory.CreateDefaultActionArgumentProcessor());
            }
        }

        /// <summary>
        /// Executes the argument processor
        /// </summary>
        /// <param name="processor">Argument processor to execute.</param>
        /// <param name="exitCode">Exit status of Argument processor</param>
        /// <returns> true if continue execution, false otherwise.</returns>
        private bool ExecuteArgumentProcessor(IArgumentProcessor processor, ref int exitCode)
        {
            var continueExecution = true;
            ArgumentProcessorResult result;
            try
            {
                result = processor.Executor.Value.Execute();
            }
            catch (Exception ex)
            {
                if (ex is CommandLineException || ex is TestPlatformException || ex is SettingsException || ex is InvalidOperationException)
                {
                    EqtTrace.Error("ExecuteArgumentProcessor: failed to execute argument process: {0}", ex);
                    this.Output.Error(false, ex.Message);
                    result = ArgumentProcessorResult.Fail;

                    // Send inner exception only when its message is different to avoid duplicate.
                    if (ex is TestPlatformException &&
                        ex.InnerException != null &&
                        !string.Equals(ex.InnerException.Message, ex.Message, StringComparison.CurrentCultureIgnoreCase))
                    {
                        this.Output.Error(false, ex.InnerException.Message);
                    }
                }
                else
                {
                    // Let it throw - User must see crash and report it with stack trace!
                    // No need for recoverability as user will start a new vstest.console anyway
                    throw;
                }
            }

            Debug.Assert(
                result >= ArgumentProcessorResult.Success && result <= ArgumentProcessorResult.Abort,
                "Invalid argument processor result.");

            if (result == ArgumentProcessorResult.Fail)
            {
                exitCode = 1;
            }

            if (result == ArgumentProcessorResult.Abort)
            {
                continueExecution = false;
            }
            return continueExecution;
        }

        /// <summary>
        /// Displays the Company and Copyright splash title info immediately after launch
        /// </summary>
        private void PrintSplashScreen(bool isDiag)
        {
            string assemblyVersion = Product.Version;
            if (!isDiag)
            {
                var end = Product.Version?.IndexOf("-release");

                if (end >= 0)
                {
                    assemblyVersion = Product.Version?.Substring(0, end.Value);
                }
            }

            string commandLineBanner = string.Format(CultureInfo.CurrentUICulture, CommandLineResources.MicrosoftCommandLineTitle, assemblyVersion);
            this.Output.WriteLine(commandLineBanner, OutputLevel.Information);
            this.Output.WriteLine(CommandLineResources.CopyrightCommandLineTitle, OutputLevel.Information);
            this.Output.WriteLine(string.Empty, OutputLevel.Information);
        }

        /// <summary>
        /// Flattens command line arguments by processing response files.
        /// </summary>
        /// <param name="arguments">Arguments provided to perform execution with.</param>
        /// <param name="flattenedArguments">Array of flattened arguments.</param>
        /// <returns>0 if successful and 1 otherwise.</returns>
        private int FlattenArguments(IEnumerable<string> arguments, out string[] flattenedArguments)
        {
            List<string> outputArguments = new List<string>();
            int result = 0;

            foreach (var arg in arguments)
            {
                if (arg.StartsWith("@", StringComparison.Ordinal))
                {
                    // response file:
                    string path = arg.Substring(1).TrimEnd(null);
                    var hadError = this.ReadArgumentsAndSanitize(path, out var responseFileArgs, out var nestedArgs);

                    if (hadError)
                    {
                        result |= 1;
                    }
                    else
                    {
                        this.Output.WriteLine(string.Format("vstest.console.exe {0}", responseFileArgs), OutputLevel.Information);
                        outputArguments.AddRange(nestedArgs);
                    }
                }
                else
                {
                    outputArguments.Add(arg);
                }
            }

            flattenedArguments = outputArguments.ToArray();
            return result;
        }

        /// <summary>
        /// Read and sanitize the arguments.
        /// </summary>
        /// <param name="fileName">File provided by user.</param>
        /// <param name="args">argument in the file as string.</param>
        /// <param name="arguments">Modified argument after sanitizing the contents of the file.</param>
        /// <returns>0 if successful and 1 otherwise.</returns>
        public bool ReadArgumentsAndSanitize(string fileName, out string args, out string[] arguments)
        {
            arguments = null;
            if (GetContentUsingFile(fileName, out args))
            {
                return true;
            }

            if (string.IsNullOrEmpty(args))
            {
                return false;
            }

            return CommandLineUtilities.SplitCommandLineIntoArguments(args, out arguments);
        }

        private bool GetContentUsingFile(string fileName, out string contents)
        {
            contents = null;
            try
            {
                contents = File.ReadAllText(fileName);
            }
            catch (Exception e)
            {
                EqtTrace.Verbose("Executor.Execute: Exiting with exit code of {0}", 1);
                EqtTrace.Error(string.Format(CultureInfo.InvariantCulture, "Error: Can't open command line argument file '{0}' : '{1}'", fileName, e.Message));
                this.Output.Error(false, string.Format(CultureInfo.CurrentCulture, CommandLineResources.OpenResponseFileError, fileName));
                return true;
            }

            return false;
        }

        #endregion
    }
}