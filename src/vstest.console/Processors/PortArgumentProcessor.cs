// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using Microsoft.VisualStudio.TestPlatform.Client.DesignMode;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine;

    using TestPlatformHelpers;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

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

        public override ArgumentProcessorPriority Priority => ArgumentProcessorPriority.DesignMode;

        public override string HelpContentResourceName => CommandLineResources.PortArgumentHelp;

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
        /// <param name="testRequestManager"> Test request manager</param>
        public PortArgumentExecutor(CommandLineOptions options, ITestRequestManager testRequestManager)
            : this(options, testRequestManager, InitializeDesignMode)
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
            if (string.IsNullOrWhiteSpace(argument) || !int.TryParse(argument, out int portNumber))
            {
                throw new CommandLineException(CommandLineResources.InvalidPortArgument);
            }

            this.commandLineOptions.Port = portNumber;
            this.commandLineOptions.IsDesignMode = true;
            this.designModeClient = this.designModeInitializer?.Invoke(this.commandLineOptions.ParentProcessId);
        }

        /// <summary>
        /// Initialize the design mode client.
        /// </summary>
        /// <returns><see cref="ArgumentProcessorResult.Success"/> if initialization is successful.</returns>
        public ArgumentProcessorResult Execute()
        {
            try
            {
                this.designModeClient?.ConnectToClientAndProcessRequests(this.commandLineOptions.Port, this.testRequestManager);
            }
            catch (TimeoutException ex)
            {
                throw new CommandLineException(string.Format(CultureInfo.CurrentUICulture, string.Format(CommandLineResources.DesignModeClientTimeoutError, this.commandLineOptions.Port)), ex);
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
