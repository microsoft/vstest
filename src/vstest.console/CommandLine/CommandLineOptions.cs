// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    using Utilities.Helpers;
    using Utilities.Helpers.Interfaces;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;
    using System.IO;

    /// <summary>
    /// Provides access to the command-line options.
    /// </summary>
    internal class CommandLineOptions
    {
        #region Constants/Readonly 
        
        /// <summary>
        /// The default batch size.
        /// </summary>
        public const long DefaultBatchSize = 10;

        /// <summary>
        /// The use vsix extensions key.
        /// </summary>
        public const string UseVsixExtensionsKey = "UseVsixExtensions";

        /// <summary>
        /// The default use vsix extensions value.
        /// </summary>
        public const bool DefaultUseVsixExtensionsValue = false;
        
        /// <summary>
        /// The default retrieval timeout for fetching of test results or test cases
        /// </summary>
        private readonly TimeSpan DefaultRetrievalTimeout = new TimeSpan(0, 0, 0, 1, 500); 
        
        #endregion
        
        #region PrivateMembers

        private static CommandLineOptions instance;

        private List<string> sources = new List<string>();

        private Architecture architecture;
        
        private Framework frameworkVersion;

        #endregion

        /// <summary>
        /// Gets the instance.
        /// </summary>
        internal static CommandLineOptions Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CommandLineOptions();
                }

                return instance;
            }
        }

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected CommandLineOptions()
        {
            this.BatchSize = DefaultBatchSize;
            this.TestStatsEventTimeout = this.DefaultRetrievalTimeout;
            this.FileHelper = new FileHelper();
#if TODO
            UseVsixExtensions = Utilities.GetAppSettingValue(UseVsixExtensionsKey, false);
#endif
        }

#endregion

        #region Properties

        /// <summary>
        /// Specifies whether parallel execution is on or off.
        /// </summary>
        public bool Parallel { get; set; }

        /// <summary>
        /// Readonly collection of all available test sources
        /// </summary>
        public IEnumerable<string> Sources
        {
            get
            {
                return this.sources.AsReadOnly();
            }
        }

        /// <summary>
        /// Specifies whether dynamic code coverage diagnostic data adapter needs to be configured.
        /// </summary>
        public bool EnableCodeCoverage { get; set; }

        /// <summary>
        /// Specifies whether the Fakes automatic configuration should be disabled.
        /// </summary>
        public bool DisableAutoFakes { get; set; } = false;

        /// <summary>
        /// Specifies whether vsixExtensions is enabled or not. 
        /// </summary>
        public bool UseVsixExtensions { get; set; }

        /// <summary>
        /// Path to the custom test adapters. 
        /// </summary>
        public string TestAdapterPath { get; set; }

        /// <summary>
        /// Process Id of the process which launched vstest runner
        /// </summary>
        public int ParentProcessId { get; set; }

        /// <summary>
        /// Port IDE process is listening to
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Configuration the project is built for e.g. Debug/Release
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// Directory containing the temporary outputs
        /// </summary>
        public string BuildBasePath { get; set; }

        /// <summary>
        /// Directory containing the binaries to run
        /// </summary>
        public string Output { get; set; }
        
        /// <summary>
        /// Specifies the frequency of the runStats/discoveredTests event
        /// </summary>
        public long BatchSize { get; set; }

        /// <summary>
        /// Specifies the timeout of the test stats cache timeout event
        /// </summary>
        public TimeSpan TestStatsEventTimeout { get; set; }

        /// <summary>
        /// Test case filter value for run with sources.
        /// </summary>
        public string TestCaseFilterValue { get; set; }

        /// <summary>
        /// Specifies the Target Device
        /// </summary>
        public string TargetDevice { get; set; }

        /// <summary>
        /// Specifies whether the target device has a Windows Phone context or not
        /// </summary>
        public bool HasPhoneContext
        {
            get
            {
                return !string.IsNullOrEmpty(TargetDevice);
            }
        }
        
        /// <summary>
        /// Specifies the target platform type for test run.
        /// </summary>
        public Architecture TargetArchitecture
        {
            get
            {
                return this.architecture;
            }
            set
            {
                this.architecture = value;
                this.ArchitectureSpecified = true;
            }
        }

        /// <summary>
        /// True indicates the test run is started from an Editor or IDE.
        /// Defaults to false.
        /// </summary>
        public bool IsDesignMode
        {
            get;
            set;
        }

        /// <summary>
        /// Specifies if /Platform has been specified on command line or not.
        /// </summary>
        internal bool ArchitectureSpecified { get; private set; }

        internal IFileHelper FileHelper { get; set; }

        /// <summary>
        /// Gets or sets the target Framework version for test run.
        /// </summary>
        internal Framework TargetFrameworkVersion
        {
            get
            {
                return this.frameworkVersion;
            }
            set
            {
                this.frameworkVersion = value;
                this.FrameworkVersionSpecified = true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether /Framework has been specified on command line or not.
        /// </summary>
        internal bool FrameworkVersionSpecified { get; private set; }

        /// <summary>
        /// Gets or sets the results directory for test run.
        /// </summary>
        internal string ResultsDirectory { get; set; }

        /// <summary>
        /// Gets or sets the /setting switch value. i.e path to settings file.
        /// </summary>
        internal string SettingsFile { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a source file to look for tests in.
        /// </summary>
        /// <param name="source">Path to source file to look for tests in.</param>
        public void AddSource(string source)
        {
            if (String.IsNullOrWhiteSpace(source))
            {
                throw new CommandLineException(CommandLineResources.CannotBeNullOrEmpty);
            }

            source = source.Trim();

            // Convert the relative path to absolute path
            if(!Path.IsPathRooted(source))
            {
                source = Path.Combine(FileHelper.GetCurrentDirectory(), source);
            }

            if (!FileHelper.Exists(source))
            {
                throw new CommandLineException(
                    string.Format(CultureInfo.CurrentUICulture, CommandLineResources.TestSourceFileNotFound, source));
            }

            if (this.sources.Contains(source, StringComparer.OrdinalIgnoreCase))
            {
                throw new CommandLineException(
                    string.Format(CultureInfo.CurrentCulture, CommandLineResources.DuplicateSource, source));
            }

            this.sources.Add(source);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Resets the options. Clears the sources.
        /// </summary>
        internal void Reset()
        {
            instance = null;
        }

        #endregion
    }
}
