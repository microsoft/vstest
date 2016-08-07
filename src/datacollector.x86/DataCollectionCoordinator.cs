// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection.Interfaces;

    /// <summary>
    /// Coordinates the Data Collection for V1 and V2 DataCollectors
    /// </summary>
    internal class DataCollectionCoordinator : IDisposable
    {
        private IDataCollectionManager[] dataCollectionManagers;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionCoordinator"/> class.
        /// </summary>
        public DataCollectionCoordinator() : this(default(IDataCollectionManager[]))
        {
        }

        /// <summary>
        /// Constructor with dependency injection. Used for unit testing.
        /// </summary>
        /// <param name="dataCollectionManagers">Array of IDataCollectionManagers for handling various versions of DataCollectors (Legacy,V2)</param>
        internal DataCollectionCoordinator(IDataCollectionManager[] dataCollectionManagers)
        {
            this.dataCollectionManagers = dataCollectionManagers;
        }

        /// <summary>
        /// Invoked before starting of test run.
        /// </summary>
        /// <param name="settingsXml">Specifies the settings which are being used for the run.</param>
        /// <param name="resetDataCollectors">Forces the data collectors to be reset.</param>
        /// <param name="isRunStartingNow">Specifies whether run is going to start immediately.</param>
        /// <returns>Enivronment variables for the executor.</returns>
        public BeforeTestRunStartResult BeforeTestRunStart(string settingsXml, bool resetDataCollectors, bool isRunStartingNow)
        {
            if (this.dataCollectionManagers == null || this.dataCollectionManagers.Length == 0)
            {
                return null;
            }

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DataCollectionCoordinator: BeforeTestRunStart Entering.");
            }

            var runSettings = RunSettingsUtilities.CreateAndInitializeRunSettings(settingsXml);

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DataCollectionCoordinator: Loading/Initializing the data collectors");
            }

            // Load the collectors and get the environment variables
            var environmentVariables = this.LoadDataCollectors(runSettings);

            var areTestCaseLevelEventsRequired = false;

            if (isRunStartingNow)
            {
                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("DataCollectionCoordinator: Raising session started event.");
                }

                // Raise SessionStart event to loaded data collection plugins.
                areTestCaseLevelEventsRequired = this.SessionStarted();
            }

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DataCollectionCoordinator: BeforeTestRunStart Exiting areTestCaseLevelEventsRequired={0}.", areTestCaseLevelEventsRequired);
            }

            // todo : Get Data Collection Port here
            return new BeforeTestRunStartResult(environmentVariables, areTestCaseLevelEventsRequired, 0);
        }

        /// <summary>
        /// Invoked after ending of test run.
        /// </summary>
        /// <param name="isCancelled">Specified whether the test run is cancelled.</param>
        /// <returns>Collection of session attachmentsets.</returns>
        public Collection<AttachmentSet> AfterTestRunEnd(bool isCancelled)
        {
            if (this.dataCollectionManagers == null || this.dataCollectionManagers.Length == 0)
            {
                return null;
            }

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DataCollectionCoordinator.AfterTestRunEnd: Entering.");
            }

            // Send RunCompleteEvent to data collection plugin manager so it can raise session end event to loaded collector plugins.
            Collection<AttachmentSet> result = this.SessionEnded(isCancelled);

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("DataCollectionCoordinator.AfterTestRunEnd: Exiting.");
            }

            return result;
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.dataCollectionManagers != null && this.dataCollectionManagers.Length > 0)
                    {
                        var tasks = new List<Task>(this.dataCollectionManagers.Length);

                        foreach (var dataCollectionManager in this.dataCollectionManagers)
                        {
                            tasks.Add(Task.Factory.StartNew(() => dataCollectionManager.Dispose()));
                        }

                        Task.WaitAll(tasks.ToArray());
                    }
                }

                this.disposed = true;
            }
        }

        private Dictionary<string, string> LoadDataCollectors(RunSettings runSettings)
        {
            var envVars = new Dictionary<string, string>();
            var tasks = new List<Task<IDictionary<string, string>>>(this.dataCollectionManagers.Length);

            foreach (var dataCollectionManager in this.dataCollectionManagers)
            {
                tasks.Add(Task<IDictionary<string, string>>.Factory.StartNew(() => dataCollectionManager.LoadDataCollectors(runSettings)));
            }

            Task.WaitAll(tasks.ToArray());

            for (var i = 0; i < this.dataCollectionManagers.Length; i++)
            {
                if (tasks[i].Status == TaskStatus.Faulted)
                {
                    throw tasks[i].Exception.InnerException;
                }

                foreach (var kvp in tasks[i]?.Result)
                {
                    if (!envVars.ContainsKey(kvp.Key))
                    {
                        envVars.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            return envVars;
        }

        private bool SessionStarted()
        {
            var areTestCaseLevelEventsRequired = false;

            var tasks = new List<Task<bool>>(this.dataCollectionManagers.Length);

            foreach (var dataCollectionManager in this.dataCollectionManagers)
            {
                tasks.Add(Task<bool>.Factory.StartNew(() => dataCollectionManager.SessionStarted()));
            }

            Task.WaitAll(tasks.ToArray());

            for (var i = 0; i < this.dataCollectionManagers.Length; i++)
            {
                areTestCaseLevelEventsRequired = areTestCaseLevelEventsRequired || tasks[i].Result;
            }

            return areTestCaseLevelEventsRequired;
        }

        private Collection<AttachmentSet> SessionEnded(bool isCancelled)
        {
            var attachments = new Collection<AttachmentSet>();
            var tasks = new List<Task<Collection<AttachmentSet>>>(this.dataCollectionManagers.Length);

            foreach (var dataCollectionManager in this.dataCollectionManagers)
            {
                tasks.Add(Task<Collection<AttachmentSet>>.Factory.StartNew(() => dataCollectionManager.SessionEnded(isCancelled)));
            }

            Task.WaitAll(tasks.ToArray());

            for (var i = 0; i < this.dataCollectionManagers.Length; i++)
            {
                if (tasks[i].Result != null)
                {
                    foreach (var attachment in tasks[i].Result)
                    {
                        attachments.Add(attachment);
                    }
                }
            }

            return attachments;
        }
    }
}
