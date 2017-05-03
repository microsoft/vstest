// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// Abstract DataCollection Launcher provides functionality to handle process launch and exit events.
    /// </summary>
    public abstract class DataCollectionLauncher : IDataCollectionLauncher
    {
        protected bool dcExitedEventRaised;

        protected IProcessHelper processHelper;

        protected IMessageLogger messageLogger;

        protected StringBuilder testHostProcessStdError;

        /// <summary>
        /// Gets or sets the error length for datacollector error stream.
        /// </summary>
        protected int ErrorLength { get; set; } = 4096;

        /// <summary>
        /// Gets or sets the Timeout for data collector to initialize.
        /// </summary>
        protected int TimeOut { get; set; } = 10000;

        public virtual Process DataCollectorProcess { get;  protected set; }

        public event EventHandler<HostProviderEventArgs> DataCollectorLaunched;
        public event EventHandler<HostProviderEventArgs> DataCollectorExited;

        public DataCollectionLauncher(IProcessHelper processHelper, IMessageLogger messageLogger)
        {
            this.processHelper = processHelper;
            this.messageLogger = messageLogger;
        }

        /// <summary>
        /// The launch data collector.
        /// </summary>
        /// <param name="environmentVariables">
        /// The environment variables.
        /// </param>
        /// <param name="commandLineArguments">
        /// The command line arguments.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public abstract int LaunchDataCollector(
            IDictionary<string, string> environmentVariables,
            IList<string> commandLineArguments);

        /// <summary>
        /// Raises DataCollector Launched event
        /// </summary>
        /// <param name="e">hostprovider event args</param>
        public void OnDataCollectorLaunched(HostProviderEventArgs e)
        {
            this.DataCollectorLaunched.SafeInvoke(this, e, "DataCollectorLauncher.OnHostLaunched");
        }

        /// <summary>
        /// Raises DCExited event
        /// </summary>
        /// <param name="e">hostprovider event args</param>
        public void OnDataCollectorExited(HostProviderEventArgs e)
        {
            if (!this.dcExitedEventRaised)
            {
                this.dcExitedEventRaised = true;
                this.DataCollectorExited.SafeInvoke(this, e, "DataCollectorLauncher.OnDataCollectorExited");
            }
        }

        /// <summary>
        /// Gets callback on process exit
        /// </summary>
        protected Action<object> ExitCallBack => (process) =>
        {
            ProcessCallbacks.ExitCallBack(this.processHelper, this.messageLogger, process, this.testHostProcessStdError, this.OnDataCollectorExited);
        };

        /// <summary>
        /// Gets callback to read from process error stream
        /// </summary>
        protected Action<object, string> ErrorReceivedCallback => (process, data) =>
        {
            ProcessCallbacks.ErrorReceivedCallback(this.testHostProcessStdError, data);
        };
    }
}
