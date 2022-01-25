// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using Microsoft.VisualStudio.TestPlatform.Client.DesignMode;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    using TestPlatformHelpers;

    using CommandLineResources = Resources.Resources;

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
                if (metadata == null)
                {
                    metadata = new Lazy<IArgumentProcessorCapabilities>(() => new PortArgumentProcessorCapabilities());
                }

                return metadata;
            }
        }

        /// <summary>
        /// Gets or sets the executor.
        /// </summary>
        public Lazy<IArgumentExecutor> Executor
        {
            get
            {
                if (executor == null)
                {
                    executor = new Lazy<IArgumentExecutor>(() =>
                    new PortArgumentExecutor(CommandLineOptions.Instance, TestRequestManager.Instance));
                }

                return executor;
            }

            set
            {
                executor = value;
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
        private readonly CommandLineOptions commandLineOptions;

        /// <summary>
        /// Test Request Manager
        /// </summary>
        private readonly ITestRequestManager testRequestManager;

        /// <summary>
        /// Initializes Design mode when called
        /// </summary>
        private readonly Func<int, IProcessHelper, IDesignModeClient> designModeInitializer;

        /// <summary>
        /// IDesignModeClient
        /// </summary>
        private IDesignModeClient designModeClient;

        /// <summary>
        /// Process helper for process management actions.
        /// </summary>
        private readonly IProcessHelper processHelper;

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
            : this(options, testRequestManager, InitializeDesignMode, new ProcessHelper())
        {
        }

        /// <summary>
        /// For Unit testing only
        /// </summary>
        internal PortArgumentExecutor(CommandLineOptions options, ITestRequestManager testRequestManager, IProcessHelper processHelper)
            : this(options, testRequestManager, InitializeDesignMode, processHelper)
        {
        }

        /// <summary>
        /// For Unit testing only
        /// </summary>
        internal PortArgumentExecutor(CommandLineOptions options, ITestRequestManager testRequestManager, Func<int, IProcessHelper, IDesignModeClient> designModeInitializer, IProcessHelper processHelper)
        {
            Contract.Requires(options != null);
            commandLineOptions = options;
            this.testRequestManager = testRequestManager;
            this.designModeInitializer = designModeInitializer;
            this.processHelper = processHelper;
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

            commandLineOptions.Port = portNumber;
            commandLineOptions.IsDesignMode = true;
            designModeClient = designModeInitializer?.Invoke(commandLineOptions.ParentProcessId, processHelper);
        }

        /// <summary>
        /// Initialize the design mode client.
        /// </summary>
        /// <returns> The <see cref="ArgumentProcessorResult.Success"/> if initialization is successful. </returns>
        public ArgumentProcessorResult Execute()
        {
            try
            {
                designModeClient?.ConnectToClientAndProcessRequests(commandLineOptions.Port, testRequestManager);
            }
            catch (TimeoutException ex)
            {
                throw new CommandLineException(string.Format(CultureInfo.CurrentUICulture, string.Format(CommandLineResources.DesignModeClientTimeoutError, commandLineOptions.Port)), ex);
            }

            return ArgumentProcessorResult.Success;
        }

        #endregion

        private static IDesignModeClient InitializeDesignMode(int parentProcessId, IProcessHelper processHelper)
        {
            if (parentProcessId > 0)
            {
                processHelper.SetExitCallback(parentProcessId, (obj) =>
                {
                    EqtTrace.Info($"PortArgumentProcessor: parent process:{parentProcessId} exited.");
                    DesignModeClient.Instance?.HandleParentProcessExit();
                });
            }

            DesignModeClient.Initialize();
            return DesignModeClient.Instance;
        }
    }
}
