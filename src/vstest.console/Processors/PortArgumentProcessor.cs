// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using Microsoft.VisualStudio.TestPlatform.CommandLine;
    using System;
    using System.Diagnostics.Contracts;
    using TestPlatformHelpers;
    using Resources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.Client.DesignMode;
    using System.Diagnostics;

    /// <summary>
    /// Argument Processor for the "--Port|/Port" command line argument.
    /// </summary>
    internal class PortArgumentProcessor : IArgumentProcessor
    {
        #region Constants

        /// <summary>
        /// The name of the command line argument that the PortArgumentExecutor handles.
        /// </summary>
        public const string CommandName = "/Port";

        #endregion

        private Lazy<IArgumentProcessorCapabilities> metadata;

        private Lazy<IArgumentExecutor> executor;

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        public Lazy<IArgumentProcessorCapabilities> Metadata
        {
            get
            {
                if (this.metadata == null)
                {
                    this.metadata = new Lazy<IArgumentProcessorCapabilities>(() => new PortArgumentProcessorCapabilities());
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
                    this.executor = new Lazy<IArgumentExecutor>(() => 
                    new PortArgumentExecutor(CommandLineOptions.Instance, TestRequestManager.Instance));
                }

                return this.executor;
            }

            set
            {
                this.executor = value;
            }
        }
    }

    internal class PortArgumentProcessorCapabilities : BaseArgumentProcessorCapabilities
    {
        public override string CommandName => PortArgumentProcessor.CommandName;

        public override bool AllowMultiple => false;

        public override bool IsAction => false;

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.Normal;

        public override string HelpContentResourceName => Resources.PortArgumentHelp;

        public override HelpContentPriority HelpPriority => HelpContentPriority.PortArgumentProcessorHelpPriority;
    }

    /// <summary>
    /// Argument Executor for the "/Port" command line argument.
    /// </summary>
    internal class PortArgumentExecutor : IArgumentExecutor
    {
        #region Fields

        /// <summary>
        /// Used for getting sources.
        /// </summary>
        private CommandLineOptions commandLineOptions;

        /// <summary>
        /// Test Request Manager
        /// </summary>
        private ITestRequestManager testRequestManager;

        /// <summary>
        /// Initializes Design mode when called
        /// </summary>
        private Func<int, IDesignModeClient> designModeInitializer;

        /// <summary>
        /// IDesignModeClient
        /// </summary>
        private IDesignModeClient designModeClient;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="options">
        /// The options.
        /// </param>
        public PortArgumentExecutor(CommandLineOptions options, ITestRequestManager testRequestManager) : 
            this (options, testRequestManager, InitializeDesignMode)
        {
        }

        /// <summary>
        /// For Unit testing only
        /// </summary>
        internal PortArgumentExecutor(CommandLineOptions options, ITestRequestManager testRequestManager, Func<int, IDesignModeClient> designModeInitializer)
        {
            Contract.Requires(options != null);
            this.commandLineOptions = options;
            this.testRequestManager = testRequestManager;
            this.designModeInitializer = designModeInitializer;
        }

        #endregion

        #region IArgumentExecutor

        /// <summary>
        /// Initializes with the argument that was provided with the command.
        /// </summary>
        /// <param name="argument">Argument that was provided with the command.</param>
        public void Initialize(string argument)
        {
            int portNumber;
            if (string.IsNullOrWhiteSpace(argument) || !int.TryParse(argument, out portNumber))
            {
                throw new CommandLineException(Resources.InvalidPortArgument);
            }

            this.commandLineOptions.Port = portNumber;
            this.designModeClient = this.designModeInitializer?.Invoke(this.commandLineOptions.ParentProcessId);
        }

        /// <summary>
        /// The port is already set, return success.
        /// </summary>
        /// <returns> The <see cref="ArgumentProcessorResult"/> Success </returns>
        public ArgumentProcessorResult Execute()
        {
            try
            {
                this.designModeClient?.ConnectToClientAndProcessRequests(this.commandLineOptions.Port, this.testRequestManager);
            }
            catch(TimeoutException)
            {
                // Todo:sasin log the exception
                return ArgumentProcessorResult.Fail;
            }
            return ArgumentProcessorResult.Success;
        }

        #endregion

        private static IDesignModeClient InitializeDesignMode(int parentProcessId)
        {
            if (parentProcessId > 0)
            {
                var process = Process.GetProcessById(parentProcessId);
                if (process != null && !process.HasExited)
                {
                    process.EnableRaisingEvents = true;
                    process.Exited += (sender, e) => DesignModeClient.Instance?.HandleParentProcessExit();
                }
            }

            DesignModeClient.Initialize();
            return DesignModeClient.Instance;
        }
    }
}
