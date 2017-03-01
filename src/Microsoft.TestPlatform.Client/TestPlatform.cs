// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Globalization;

    using Microsoft.VisualStudio.TestPlatform.Client.Discovery;
    using Microsoft.VisualStudio.TestPlatform.Client.Execution;
    using ClientResources = Microsoft.VisualStudio.TestPlatform.Client.Resources.Resources;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    /// <summary>
    /// Implementation for TestPlatform
    /// </summary>
    public class TestPlatform : ITestPlatform
    {
        protected IFileHelper fileHelper;
        /// <summary>
        /// Initializes a new instance of the <see cref="TestPlatform"/> class.
        /// </summary>
        public TestPlatform() : this(new TestEngine())
        {
            fileHelper = new FileHelper();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestPlatform"/> class.
        /// </summary>
        /// <param name="testEngine">
        /// The test engine.
        /// </param>
        protected TestPlatform(ITestEngine testEngine)
        {
            this.TestEngine = testEngine;
        }

        /// <summary>
        /// Gets or sets Test Engine instance
        /// </summary>
        private ITestEngine TestEngine { get; set; }

        /// <summary>
        /// The create discovery request.
        /// </summary>
        /// <param name="discoveryCriteria"> The discovery criteria. </param>
        /// <returns> The <see cref="IDiscoveryRequest"/>. </returns>
        /// <exception cref="ArgumentNullException"> Throws if parameter is null. </exception>
        public IDiscoveryRequest CreateDiscoveryRequest(DiscoveryCriteria discoveryCriteria)
        {
            if (discoveryCriteria == null)
            {
                throw new ArgumentNullException("discoveryCriteria");
            }

            UpdateTestAdapterPaths(discoveryCriteria.RunSettings);

            var runconfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(discoveryCriteria.RunSettings);
            var testHostManager = this.TestEngine.GetDefaultTestHostManager(runconfiguration);

            var discoveryManager = this.TestEngine.GetDiscoveryManager(testHostManager, discoveryCriteria);
            discoveryManager.Initialize();

            return new DiscoveryRequest(discoveryCriteria, discoveryManager);
        }

        /// <summary>
        /// The create test run request.
        /// </summary>
        /// <param name="testRunCriteria"> The test run criteria.  </param>
        /// <returns> The <see cref="ITestRunRequest"/>. </returns>
        /// <exception cref="ArgumentNullException"> Throws if parameter is null. </exception>
        public ITestRunRequest CreateTestRunRequest(TestRunCriteria testRunCriteria)
        {
            if (testRunCriteria == null)
            {
                throw new ArgumentNullException("testRunCriteria");
            }

            UpdateTestAdapterPaths(testRunCriteria.TestRunSettings);

            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(testRunCriteria.TestRunSettings);

            // Update and initialize loggers only when DesignMode is false
            if (runConfiguration.DesignMode == false)
            {
                UpdateAndInitializeLoggers(testRunCriteria);
            }

            var testHostManager = this.TestEngine.GetDefaultTestHostManager(runConfiguration);

            if (testRunCriteria.TestHostLauncher != null)
            {
                testHostManager.SetCustomLauncher(testRunCriteria.TestHostLauncher);
            }

            var executionManager = this.TestEngine.GetExecutionManager(testHostManager, testRunCriteria);
            executionManager.Initialize();

            return new TestRunRequest(testRunCriteria, executionManager);
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The initialize.
        /// </summary>
        /// <param name="pathToAdditionalExtensions"> The path to additional extensions. </param>
        /// <param name="loadOnlyWellKnownExtensions"> The load only well known extensions. </param>
        /// <param name="forceX86Discoverer"> The force x86 discoverer. </param>
        public void Initialize(IEnumerable<string> pathToAdditionalExtensions, bool loadOnlyWellKnownExtensions, bool forceX86Discoverer)
        {
            // TODO: ForceX86Discoverer options
            this.TestEngine.GetExtensionManager()
                 .UseAdditionalExtensions(pathToAdditionalExtensions, loadOnlyWellKnownExtensions);
        }

        /// <summary>
        /// The update extensions.
        /// </summary>
        /// <param name="pathToAdditionalExtensions"> The path to additional extensions. </param>
        /// <param name="loadOnlyWellKnownExtensions"> The load only well known extensions. </param>
        public void UpdateExtensions(IEnumerable<string> pathToAdditionalExtensions, bool loadOnlyWellKnownExtensions)
        {
            this.TestEngine.GetExtensionManager()
                   .UseAdditionalExtensions(pathToAdditionalExtensions, loadOnlyWellKnownExtensions);
        }

        /// <summary>
        /// Update the test adapter paths provided through run settings to be used by the test service
        /// </summary>
        private void UpdateTestAdapterPaths(string runSettings)
        {
            IEnumerable<string> customTestAdaptersPaths = RunSettingsUtilities.GetTestAdaptersPaths(runSettings);

            if (customTestAdaptersPaths != null)
            {
                foreach (string customTestAdaptersPath in customTestAdaptersPaths)
                {
                    var adapterPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(customTestAdaptersPath));
                    if (!Directory.Exists(adapterPath))
                    {
                        EqtTrace.Warning(string.Format("AdapterPath Not Found:", adapterPath));
                        continue;
                    }
                    List<string> adapterFiles = new List<string>(
                        this.fileHelper.EnumerateFiles(adapterPath, TestPlatformConstants.TestAdapterResxPattern, SearchOption.AllDirectories)
                        );
                    if (adapterFiles.Count > 0)
                    {
                        this.UpdateExtensions(adapterFiles, true);
                    }
                }
            }
        }

        /// <summary>
        /// Update the test adapter paths provided through run settings to be used by the test service
        /// </summary>
        private void UpdateAndInitializeLoggers(TestRunCriteria testRunCriteria)
        {
            this.UpdateTestLoggerPath(testRunCriteria);

            // Initialize Loggers
            TestLoggerManager loggerManager = TestLoggerManager.Instance;

            foreach (var logger in LoggerList.Instance.Loggers)
            {
                string loggerIdentifier = logger.loggerIdentifier;
                Dictionary<string, string> parameters = logger.parameters;

                // First assume the logger is specified by URI. If that fails try with friendly name.
                try
                {
                    this.AddLoggerByUri(loggerManager, loggerIdentifier, parameters);
                }
                catch (InvalidLoggerException)
                {
                    string loggerUri;
                    if (loggerManager.TryGetUriFromFriendlyName(loggerIdentifier, out loggerUri))
                    {
                        this.AddLoggerByUri(loggerManager, loggerUri, parameters);
                    }
                    else
                    {
                        throw new InvalidLoggerException(
                        String.Format(
                        CultureInfo.CurrentUICulture,
                        ClientResources.LoggerNotFound,
                        logger.arument));
                    }
                }
            }
        }

        private void AddLoggerByUri(TestLoggerManager loggerManager, string argument, Dictionary<string, string> parameters)
        {
            // Get the uri and if it is not valid, throw.
            Uri loggerUri = null;
            try
            {
                loggerUri = new Uri(argument);
            }
            catch (UriFormatException)
            {
                throw new InvalidLoggerException(
                string.Format(
                    CultureInfo.CurrentUICulture,
                    ClientResources.LoggerUriInvalid,
                    argument));
            }

            // Add the logger and if it is a non-existent logger, throw.
            try
            {
                loggerManager.AddLogger(loggerUri, parameters);
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidLoggerException(e.Message, e);
            }
        }

        private void UpdateTestLoggerPath(TestRunCriteria testRunCriteria)
        {
            List<string> loggersToUpdate = new List<string>();

            var sources = testRunCriteria.Sources;
            foreach (var source in sources)
            {
                var sourceDirectory = Path.GetDirectoryName(source);
                if (!string.IsNullOrEmpty(sourceDirectory) && this.fileHelper.DirectoryExists(sourceDirectory))
                {
                    loggersToUpdate.AddRange(this.fileHelper.EnumerateFiles(sourceDirectory, TestPlatformConstants.TestLoggerResxPattern, SearchOption.TopDirectoryOnly).ToList());
                }
            }

            if (loggersToUpdate.Count > 0)
            {
                this.UpdateExtensions(loggersToUpdate, true);
            }
        }
    }
}
