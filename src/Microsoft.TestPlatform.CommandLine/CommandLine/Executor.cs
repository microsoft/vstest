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
//   If throws during validaton, output error and exit.
//   If the default (RunTests) command processor has no test containers output an error and exit
//   If the default (RunTests) command processor has no tests to run output an error and exit

// Commands metadata:
//  *Command line argument.
//   Priority.
//   Help output.
//   Required
//   Single or multiple

namespace Microsoft.TestPlatform.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Microsoft.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    using CommandLineResources = Microsoft.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    /// Performs the execution based on the arguments provided.
    /// </summary>
    internal class Executor
    {
        private ITestPlatformEventSource testPlatformEventSource;

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
        /// Exit Codes - Zero (for sucessful command execution), One (for bad command) 
        /// </returns>
        internal int Execute(params string[] args)
        {
            this.testPlatformEventSource.VsTestConsoleStart();

            int exitCode = 0;

            // If we have no arguments, set exit code to 1, add a message, and include the help processor in the args.
            if (args == null || args.Length == 0 || args.Any(string.IsNullOrWhiteSpace))
            {
                args = args ?? new string[0];
                exitCode = 1;

                // Do not add help processor as we will go and try to check for project.json files in current dir
            }

            this.PrintSplashScreen();

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
                    if (ex is CommandLineException || ex is TestPlatformException)
                    {
                        this.Output.Error(false, ex.Message);
                        result = 1;
                    }
                    else
                    {
                        // Let it throw - User must see crash and report it with stack trace!
                        // No need for recoverability as user will start a new vstest.console anwyay
                        throw;
                    }
                }
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
                if (ex is CommandLineException || ex is TestPlatformException)
                {
                    EqtTrace.Error("ExecuteArgumentProcessor: failed to execute argument process: {0}", ex);
                    this.Output.Error(false, ex.Message);
                    result = ArgumentProcessorResult.Fail;
                }
                else
                {
                    // Let it throw - User must see crash and report it with stack trace!
                    // No need for recoverability as user will start a new vstest.console anwyay
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
        private void PrintSplashScreen()
        {
            var assembly = typeof(Executor).GetTypeInfo().Assembly;
            string assemblyVersion = string.Empty;

            assemblyVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
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
        /// <see href="https://github.com/dotnet/roslyn/blob/bcdcafc2d407635bc7de205d63d0182e81ef9faa/src/Compilers/Core/Portable/CommandLine/CommonCommandLineParser.cs#L297"/>
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
                    result |= ParseResponseFile(path, out var responseFileArguments);
                    outputArguments.AddRange(responseFileArguments.Reverse());
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
        /// Parse a response file into a set of arguments. Errors opening the response file are output as errors.
        /// </summary>
        /// <param name="fullPath">Full path to the response file.</param>
        /// <param name="responseFileArguments">Enumeration of the response file arguments.</param>
        /// <returns>0 if successful and 1 otherwise.</returns>
        /// <see href="https://github.com/dotnet/roslyn/blob/bcdcafc2d407635bc7de205d63d0182e81ef9faa/src/Compilers/Core/Portable/CommandLine/CommonCommandLineParser.cs#L517"/>
        private int ParseResponseFile(string fullPath, out IEnumerable<string> responseFileArguments)
        {
            int result = 0;
            List<string> lines = new List<string>();
            try
            {
                using (var reader = new StreamReader(
                    new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read),
                                   detectEncodingFromByteOrderMarks: true))
                {
                    string str;
                    while ((str = reader.ReadLine()) != null)
                    {
                        lines.Add(str);
                    }
                }

                responseFileArguments = ParseResponseLines(lines);
            }
            catch (Exception)
            {
                this.Output.Error(false, string.Format(CultureInfo.CurrentCulture, CommandLineResources.OpenResponseFileError, fullPath));
                responseFileArguments = new string[0];
                result = 1;
            }

            return result;
        }

        /// <summary>
        /// Take a string of lines from a response file, remove comments,
        /// and split into a set of command line arguments.
        /// </summary>
        /// <see href="https://github.com/dotnet/roslyn/blob/bcdcafc2d407635bc7de205d63d0182e81ef9faa/src/Compilers/Core/Portable/CommandLine/CommonCommandLineParser.cs#L545"/>
        private static IEnumerable<string> ParseResponseLines(IEnumerable<string> lines)
        {
            List<string> arguments = new List<string>();

            foreach (string line in lines)
            {
                arguments.AddRange(CommandLineUtilities.SplitCommandLineIntoArguments(line, removeHashComments: true));
            }

            return arguments;
        }

        #endregion
    }
}
