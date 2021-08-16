// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Used to create the appropriate instance of an argument processor.
    /// </summary>
    internal class ArgumentProcessorFactory
    {
        #region Constants

        /// <summary>
        /// The command starter.
        /// </summary>
        internal const string CommandStarter = "/";

        /// <summary>
        /// The xplat command starter.
        /// </summary>
        internal const string XplatCommandStarter = "-";

        #endregion

        #region Fields

        /// <summary>
        /// Available argument processors.
        /// </summary>
        private readonly IEnumerable<IArgumentProcessor> argumentProcessors;
        private Dictionary<string, IArgumentProcessor> commandToProcessorMap;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the argument processor factory.
        /// </summary>
        /// <param name="argumentProcessors">
        /// The argument Processors.
        /// </param>
        /// <remarks>
        /// This is not public because the static Create method should be used to access the instance.
        /// </remarks>
        protected ArgumentProcessorFactory(IEnumerable<IArgumentProcessor> argumentProcessors)
        {
            Contract.Requires(argumentProcessors != null);
            this.argumentProcessors = argumentProcessors;
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Creates ArgumentProcessorFactory.
        /// </summary>
        /// <returns>ArgumentProcessorFactory.</returns>
        internal static ArgumentProcessorFactory Create()
        {
            // Get the ArgumentProcessorFactory
            return new ArgumentProcessorFactory(DefaultArgumentProcessors);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns all of the available argument processors.
        /// </summary>
        public IEnumerable<IArgumentProcessor> AllArgumentProcessors
        {
            get { return argumentProcessors; }
        }

        /// <summary>
        /// Gets a mapping between command and Argument Executor.
        /// </summary>
        internal Dictionary<string, IArgumentProcessor> CommandToProcessorMap
        {
            get
            {
                // Build the mapping if it does not already exist.
                if (this.commandToProcessorMap == null)
                {
                    BuildCommandMaps();
                }

                return this.commandToProcessorMap;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates the argument processor associated with the provided command line argument.
        /// The Lazy that is returned will initialize the underlying argument processor when it is first accessed.
        /// </summary>
        /// <param name="argument">Command line argument to create the argument processor for.</param>
        /// <returns>The argument processor or null if one was not found.</returns>
        public IArgumentProcessor CreateArgumentProcessor(string argument)
        {
            if (String.IsNullOrWhiteSpace(argument))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(argument));
            }
            Contract.EndContractBlock();

            // Parse the input into its command and argument parts.
            var pair = new CommandArgumentPair(argument);

            // Find the associated argument processor.
            IArgumentProcessor argumentProcessor;
            CommandToProcessorMap.TryGetValue(pair.Command, out argumentProcessor);

            // If an argument processor was not found for the command, then consider it as a test source argument.
            // Special commands cannot be invoked directly and are therefore ignored.
            if (argumentProcessor == null || argumentProcessor.Metadata.Value.IsSpecialCommand)
            {
                // Update the command pair since the command is actually the argument in the case of
                // a test source.
                pair = new CommandArgumentPair(TestSourceArgumentProcessor.CommandName, argument);

                argumentProcessor = CommandToProcessorMap[TestSourceArgumentProcessor.CommandName];
            }

            if (argumentProcessor != null)
            {
                argumentProcessor = WrapLazyProcessorToInitializeOnInstantiation(argumentProcessor, pair.Argument);
            }

            return argumentProcessor;
        }

        /// <summary>
        /// Creates the argument processor associated with the provided command line argument.
        /// The Lazy that is returned will initialize the underlying argument processor when it is first accessed.
        /// </summary>
        /// <param name="command">Command name of the argument processor.</param>
        /// <param name="arguments">Command line arguments to create the argument processor for.</param>
        /// <returns>The argument processor or null if one was not found.</returns>
        public IArgumentProcessor CreateArgumentProcessor(string command, string[] arguments)
        {
            if (arguments == null || arguments.Length == 0)
            {
                throw new ArgumentException("Cannot be null or empty", nameof(arguments));
            }
            Contract.EndContractBlock();

            // Find the associated argument processor.
            IArgumentProcessor argumentProcessor;
            CommandToProcessorMap.TryGetValue(command, out argumentProcessor);

            if (argumentProcessor != null)
            {
                argumentProcessor = WrapLazyProcessorToInitializeOnInstantiation(argumentProcessor, arguments);
            }

            return argumentProcessor;
        }

        /// <summary>
        /// Creates the default action argument processor.
        /// The Lazy that is returned will initialize the underlying argument processor when it is first accessed.
        /// </summary>
        /// <returns>The default action argument processor.</returns>
        public IArgumentProcessor CreateDefaultActionArgumentProcessor()
        {
            var argumentProcessor = CommandToProcessorMap[RunTestsArgumentProcessor.CommandName];
            return WrapLazyProcessorToInitializeOnInstantiation(argumentProcessor);
        }

        /// <summary>
        /// Gets the distinct (by Type) argument processors that are tagged to be always executed.
        /// The Lazy's that are returned will initialize the underlying argument processor when first accessed.
        /// </summary>
        /// <returns>The argument processors that are tagged to be always executed.</returns>
        public IEnumerable<IArgumentProcessor> GetArgumentProcessorsToAlwaysExecute()
        {
            return CommandToProcessorMap.Values
                .Where(argProcMap => argProcMap.Metadata.Value.AlwaysExecute)
                .GroupBy(argProcMap => argProcMap.GetType())
                .Select(group => WrapLazyProcessorToInitializeOnInstantiation(group.First(), (string)null));
        }

        #endregion

        #region Private Methods

        private static IEnumerable<IArgumentProcessor> DefaultArgumentProcessors => new List<IArgumentProcessor> {
                new HelpArgumentProcessor(),
                new TestSourceArgumentProcessor(),
                new ListTestsArgumentProcessor(),
                new RunTestsArgumentProcessor(),
                new RunSpecificTestsArgumentProcessor(),
                new TestAdapterPathArgumentProcessor(),
                new TestCaseFilterArgumentProcessor(),
                new ParentProcessIdArgumentProcessor(),
                new PortArgumentProcessor(),
                new RunSettingsArgumentProcessor(),
                new PlatformArgumentProcessor(),
                new FrameworkArgumentProcessor(),
                new EnableLoggerArgumentProcessor(),
                new ParallelArgumentProcessor(),
                new EnableDiagArgumentProcessor(),
                new CLIRunSettingsArgumentProcessor(),
                new ResultsDirectoryArgumentProcessor(),
                new InIsolationArgumentProcessor(),
                new CollectArgumentProcessor(),
                new EnableCodeCoverageArgumentProcessor(),
                new DisableAutoFakesArgumentProcessor(),
                new ResponseFileArgumentProcessor(),
                new EnableBlameArgumentProcessor(),
                new UseVsixExtensionsArgumentProcessor(),
                new ListDiscoverersArgumentProcessor(),
                new ListExecutorsArgumentProcessor(),
                new ListLoggersArgumentProcessor(),
                new ListSettingsProvidersArgumentProcessor(),
                new ListFullyQualifiedTestsArgumentProcessor(),
                new ListTestsTargetPathArgumentProcessor(),
                new EnvironmentArgumentProcessor()
        };

        /// <summary>
        /// Builds the command to processor map and special command to processor map.
        /// </summary>
        private void BuildCommandMaps()
        {
            this.commandToProcessorMap = new Dictionary<string, IArgumentProcessor>(StringComparer.OrdinalIgnoreCase);

            foreach (IArgumentProcessor argumentProcessor in this.argumentProcessors)
            {
                string commandName = argumentProcessor.Metadata.Value.CommandName;
                commandToProcessorMap.Add(commandName, argumentProcessor);

                // Add xplat name for the command name. Ignore special commands
                if (!argumentProcessor.Metadata.Value.IsSpecialCommand)
                {
                    commandName = string.Concat("--", commandName.Remove(0, 1));
                    commandToProcessorMap.Add(commandName, argumentProcessor);

                    if (!string.IsNullOrEmpty(argumentProcessor.Metadata.Value.ShortCommandName))
                    {
                        string shortCommandName = argumentProcessor.Metadata.Value.ShortCommandName;
                        commandToProcessorMap.Add(shortCommandName, argumentProcessor);

                        // Add xplat short name for the command name
                        shortCommandName = shortCommandName.Replace('/', '-');
                        commandToProcessorMap.Add(shortCommandName, argumentProcessor);
                    }
                }
            }
        }

        /// <summary>
        /// Decorates a lazy argument processor so that the real processor is initialized when the lazy value is obtained.
        /// </summary>
        /// <param name="processor">The lazy processor.</param>
        /// <param name="initArg">The argument with which the real processor should be initialized.</param>
        /// <returns>The decorated lazy processor.</returns>
        private static IArgumentProcessor WrapLazyProcessorToInitializeOnInstantiation(
            IArgumentProcessor processor,
            string initArg = null)
        {
            var processorExecutor = processor.Executor;
            var lazyArgumentProcessor = new Lazy<IArgumentExecutor>(() =>
            {
                IArgumentExecutor instance = null;
                try
                {
                    instance = processorExecutor.Value;
                }
                catch (Exception e)
                {
                    if (EqtTrace.IsErrorEnabled)
                    {
                        EqtTrace.Error("ArgumentProcessorFactory.WrapLazyProcessorToInitializeOnInstantiation: Exception creating argument processor: {0}", e);
                    }
                    throw;
                }

                if (instance != null)
                {
                    try
                    {
                        instance.Initialize(initArg);
                    }
                    catch (Exception e)
                    {
                        if (EqtTrace.IsErrorEnabled)
                        {
                            EqtTrace.Error("ArgumentProcessorFactory.WrapLazyProcessorToInitializeOnInstantiation: Exception initializing argument processor: {0}", e);
                        }
                        throw;
                    }
                }

                return instance;
            }, System.Threading.LazyThreadSafetyMode.PublicationOnly);
            processor.Executor = lazyArgumentProcessor;

            return processor;
        }

        /// <summary>
        /// Decorates a lazy argument processor so that the real processor is initialized when the lazy value is obtained.
        /// </summary>
        /// <param name="processor">The lazy processor.</param>
        /// <param name="initArg">The argument with which the real processor should be initialized.</param>
        /// <returns>The decorated lazy processor.</returns>
        private static IArgumentProcessor WrapLazyProcessorToInitializeOnInstantiation(
            IArgumentProcessor processor,
            string[] initArgs)
        {
            var processorExecutor = processor.Executor;
            var lazyArgumentProcessor = new Lazy<IArgumentExecutor>(() =>
            {
                IArgumentsExecutor instance = null;
                try
                {
                    instance = (IArgumentsExecutor)processorExecutor.Value;
                }
                catch (Exception e)
                {
                    if (EqtTrace.IsErrorEnabled)
                    {
                        EqtTrace.Error("ArgumentProcessorFactory.WrapLazyProcessorToInitializeOnInstantiation: Exception creating argument processor: {0}", e);
                    }
                    throw;
                }

                if (instance != null)
                {
                    try
                    {
                        instance.Initialize(initArgs);
                    }
                    catch (Exception e)
                    {
                        if (EqtTrace.IsErrorEnabled)
                        {
                            EqtTrace.Error("ArgumentProcessorFactory.WrapLazyProcessorToInitializeOnInstantiation: Exception initializing argument processor: {0}", e);
                        }
                        throw;
                    }
                }
                return instance;
            }, System.Threading.LazyThreadSafetyMode.PublicationOnly);
            processor.Executor = lazyArgumentProcessor;

            return processor;
        }

        #endregion
    }
}