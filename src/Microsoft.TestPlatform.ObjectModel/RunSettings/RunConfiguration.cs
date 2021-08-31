// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Xml;

    /// <summary>
    /// Stores information about a test settings.
    /// </summary>
    public class RunConfiguration : TestRunSettings
    {
        #region Private Fields

        /// <summary>
        /// Platform architecture which rocksteady should use for discovery/execution
        /// </summary>
        private Architecture platform;

        /// <summary>
        /// Maximum number of cores that the engine can use to run tests in parallel
        /// </summary>
        private int maxCpuCount;

        /// <summary>
        /// .Net framework which rocksteady should use for discovery/execution
        /// </summary>
        private Framework framework;

        /// <summary>
        /// Specifies the frequency of the runStats/discoveredTests event
        /// </summary>
        private long batchSize;

        /// <summary>
        /// Specifies the Test Session Timeout in milliseconds
        /// </summary>
        private long testSessionTimeout;

        /// <summary>
        /// Directory in which rocksteady/adapter should keep their run specific data.
        /// </summary>
        private string resultsDirectory;

        /// <summary>
        /// Paths at which rocksteady should look for test adapters
        /// </summary>
        private string testAdaptersPaths;

        /// <summary>
        /// Indication to adapters to disable app domain.
        /// </summary>
        private bool disableAppDomain;

        /// <summary>
        /// Indication to adapters to disable parallelization.
        /// </summary>
        private bool disableParallelization;

        /// <summary>
        /// True if test run is triggered
        /// </summary>
        private bool designMode;

        /// <summary>
        /// Specify to run tests in isolation
        /// </summary>
        private bool inIsolation;

        /// <summary>
        /// False indicates that the test adapter should not collect source information for discovered tests
        /// </summary>
        private bool shouldCollectSourceInformation;

        /// <summary>
        /// Gets the targetDevice IP for UWP app deployment
        /// </summary>
        private string targetDevice;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RunConfiguration"/> class.
        /// </summary>
        public RunConfiguration() : base(Constants.RunConfigurationSettingsName)
        {
            // Set defaults for target platform, framework version type and results directory.
            this.platform = Constants.DefaultPlatform;
            this.framework = Framework.DefaultFramework;
            this.resultsDirectory = Constants.DefaultResultsDirectory;
            this.SolutionDirectory = null;
            this.TreatTestAdapterErrorsAsWarnings = Constants.DefaultTreatTestAdapterErrorsAsWarnings;
            this.BinariesRoot = null;
            this.testAdaptersPaths = null;
            this.maxCpuCount = Constants.DefaultCpuCount;
            this.batchSize = Constants.DefaultBatchSize;
            this.testSessionTimeout = 0;
            this.disableAppDomain = false;
            this.disableParallelization = false;
            this.designMode = false;
            this.inIsolation = false;
            this.shouldCollectSourceInformation = false;
            this.targetDevice = null;
            this.ExecutionThreadApartmentState = Constants.DefaultExecutionThreadApartmentState;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the solution directory.
        /// </summary>
        public string SolutionDirectory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the results directory.
        /// </summary>
        public string ResultsDirectory
        {
            get
            {
                return this.resultsDirectory;
            }

            set
            {
                this.resultsDirectory = value;
                this.ResultsDirectorySet = true;
            }
        }

        /// <summary>
        /// Gets or sets the Parallel execution option. Should be non-negative integer.
        /// </summary>
        public int MaxCpuCount
        {
            get
            {
                return this.maxCpuCount;
            }
            set
            {
                this.maxCpuCount = value;
                this.MaxCpuCountSet = true;
            }
        }

        /// <summary>
        /// Gets or sets the frequency of the runStats/discoveredTests event. Should be non-negative integer.
        /// </summary>
        public long BatchSize
        {
            get
            {
                return this.batchSize;
            }
            set
            {
                this.batchSize = value;
                this.BatchSizeSet = true;
            }
        }

        /// <summary>
        /// Gets or sets the testSessionTimeout. Should be non-negative integer.
        /// </summary>
        public long TestSessionTimeout
        {
            get
            {
                return this.testSessionTimeout;
            }
            set
            {
                this.testSessionTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the design mode value.
        /// </summary>
        public bool DesignMode
        {
            get
            {
                return this.designMode;
            }

            set
            {
                this.designMode = value;
                this.DesignModeSet = true;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to run tests in isolation or not.
        /// </summary>
        public bool InIsolation
        {
            get
            {
                return this.inIsolation;
            }

            set
            {
                this.inIsolation = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether test adapter needs to collect source information for discovered tests
        /// </summary>
        public bool ShouldCollectSourceInformation
        {
            get
            {
                return (this.CollectSourceInformationSet) ? this.shouldCollectSourceInformation : this.designMode;
            }

            set
            {
                this.shouldCollectSourceInformation = value;
                this.CollectSourceInformationSet = true;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether app domain creation should be disabled.
        /// </summary>
        public bool DisableAppDomain
        {
            get
            {
                return this.disableAppDomain;
            }

            set
            {
                this.disableAppDomain = value;
                this.DisableAppDomainSet = true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether parallelism needs to be disabled by the adapters.
        /// </summary>
        public bool DisableParallelization
        {
            get
            {
                return this.disableParallelization;
            }

            set
            {
                this.disableParallelization = value;
                this.DisableParallelizationSet = true;
            }
        }

        /// <summary>
        /// Gets or sets the Target platform this run is targeting. Possible values are <c>x86|x64|arm|anycpu</c>.
        /// </summary>
        public Architecture TargetPlatform
        {
            get
            {
                return this.platform;
            }

            set
            {
                this.platform = value;
                this.TargetPlatformSet = true;
            }
        }

        /// <summary>
        /// Gets or sets the target Framework this run is targeting.
        /// </summary>
        public Framework TargetFramework
        {
            get
            {
                return this.framework;
            }

            set
            {
                this.framework = value;
                this.TargetFrameworkSet = true;
            }
        }
        /// <summary>
        /// Gets or sets value indicating exit code when no tests are discovered or executed
        /// </summary>
        public bool TreatNoTestsAsError 
        { 
            get; 
            set; 
        }

        /// <summary>
        /// Gets or sets the target Framework this run is targeting. Possible values are Framework3.5|Framework4.0|Framework4.5
        /// </summary>
        [Obsolete("Use TargetFramework instead")]
        public FrameworkVersion TargetFrameworkVersion
        {
            get
            {
                switch (this.framework?.Name)
                {
                    case Constants.DotNetFramework35:
                        return FrameworkVersion.Framework35;
                    case Constants.DotNetFramework40:
                        return FrameworkVersion.Framework40;
                    case Constants.DotNetFramework45:
                        return FrameworkVersion.Framework45;
                    case Constants.DotNetFrameworkCore10:
                        return FrameworkVersion.FrameworkCore10;
                    case Constants.DotNetFrameworkUap10:
                        return FrameworkVersion.FrameworkUap10;
                    default:
                        return Constants.DefaultFramework;
                }
            }

            set
            {
                this.framework = Framework.FromString(value.ToString());
                this.TargetFrameworkSet = true;
            }
        }

        /// <summary>
        /// Gets or sets the target device IP. For Phone this value is Device, for emulators "Mobile Emulator 10.0.15063.0 WVGA 4 inch 1GB"
        /// </summary>
        public string TargetDevice
        {
            get
            {
                return this.targetDevice;
            }

            set
            {
                this.targetDevice = value;
            }
        }

        /// <summary>
        /// Gets or sets the paths used for test adapters lookup in test platform.
        /// </summary>
        public string TestAdaptersPaths
        {
            get
            {
                return this.testAdaptersPaths;
            }

            set
            {
                this.testAdaptersPaths = value;

                if (this.testAdaptersPaths != null)
                {
                    this.TestAdaptersPathsSet = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the execution thread apartment state.
        /// </summary>
        [CLSCompliant(false)]
        public PlatformApartmentState ExecutionThreadApartmentState
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to treat the errors from test adapters as warnings.
        /// </summary>
        public bool TreatTestAdapterErrorsAsWarnings
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether target platform set.
        /// </summary>
        public bool TargetPlatformSet
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether maximum parallelization count is set.
        /// </summary>
        public bool MaxCpuCountSet
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating batch size is set
        /// </summary>
        public bool BatchSizeSet
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether design mode is set.
        /// </summary>
        public bool DesignModeSet
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether disable appdomain is set.
        /// </summary>
        public bool DisableAppDomainSet
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether parallelism needs to be disabled by the adapters.
        /// </summary>
        public bool DisableParallelizationSet
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether target framework set.
        /// </summary>
        public bool TargetFrameworkSet
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether test adapters paths set.
        /// </summary>
        public bool TestAdaptersPathsSet
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether results directory is set.
        /// </summary>
        public bool ResultsDirectorySet
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the binaries root.
        /// </summary>
        public string BinariesRoot { get; private set; }

        /// <summary>
        /// Collect source information
        /// </summary>
        public bool CollectSourceInformationSet { get; private set; } = false;

        /// <summary>
        /// Default filter to use to filter tests
        /// </summary>
        public string TestCaseFilter { get; private set; }

        /// Path to dotnet executable to be used to invoke testhost.dll. Specifying this will skip looking up testhost.exe and will force usage of the testhost.dll. 
        /// </summary>
        public string DotnetHostPath { get; private set; }

        #endregion

#if !NETSTANDARD1_0
        /// <inheritdoc/>
        [SuppressMessage("Microsoft.Security.Xml", "CA3053:UseXmlSecureResolver",
            Justification = "XmlDocument.XmlResolver is not available in core. Suppress until fxcop issue is fixed.")]
        public override XmlElement ToXml()
        {
            XmlDocument doc = new XmlDocument();

            XmlElement root = doc.CreateElement(Constants.RunConfigurationSettingsName);

            XmlElement resultDirectory = doc.CreateElement("ResultsDirectory");
            resultDirectory.InnerXml = this.ResultsDirectory;
            root.AppendChild(resultDirectory);

            XmlElement targetPlatform = doc.CreateElement("TargetPlatform");
            targetPlatform.InnerXml = this.TargetPlatform.ToString();
            root.AppendChild(targetPlatform);

            XmlElement maxCpuCount = doc.CreateElement("MaxCpuCount");
            maxCpuCount.InnerXml = this.MaxCpuCount.ToString();
            root.AppendChild(maxCpuCount);

            XmlElement batchSize = doc.CreateElement("BatchSize");
            batchSize.InnerXml = this.BatchSize.ToString();
            root.AppendChild(batchSize);

            XmlElement testSessionTimeout = doc.CreateElement("TestSessionTimeout");
            testSessionTimeout.InnerXml = this.TestSessionTimeout.ToString();
            root.AppendChild(testSessionTimeout);

            XmlElement designMode = doc.CreateElement("DesignMode");
            designMode.InnerXml = this.DesignMode.ToString();
            root.AppendChild(designMode);

            XmlElement inIsolation = doc.CreateElement("InIsolation");
            inIsolation.InnerXml = this.InIsolation.ToString();
            root.AppendChild(inIsolation);

            XmlElement collectSourceInformation = doc.CreateElement("CollectSourceInformation");
            collectSourceInformation.InnerXml = this.ShouldCollectSourceInformation.ToString();
            root.AppendChild(collectSourceInformation);

            XmlElement disableAppDomain = doc.CreateElement("DisableAppDomain");
            disableAppDomain.InnerXml = this.DisableAppDomain.ToString();
            root.AppendChild(disableAppDomain);

            XmlElement disableParallelization = doc.CreateElement("DisableParallelization");
            disableParallelization.InnerXml = this.DisableParallelization.ToString();
            root.AppendChild(disableParallelization);

            XmlElement targetFrameworkVersion = doc.CreateElement("TargetFrameworkVersion");
            targetFrameworkVersion.InnerXml = this.TargetFramework.ToString();
            root.AppendChild(targetFrameworkVersion);

            XmlElement executionThreadApartmentState = doc.CreateElement("ExecutionThreadApartmentState");
            executionThreadApartmentState.InnerXml = this.ExecutionThreadApartmentState.ToString();
            root.AppendChild(executionThreadApartmentState);

            if (this.TestAdaptersPaths != null)
            {
                XmlElement testAdaptersPaths = doc.CreateElement("TestAdaptersPaths");
                testAdaptersPaths.InnerXml = this.TestAdaptersPaths;
                root.AppendChild(testAdaptersPaths);
            }

            XmlElement treatTestAdapterErrorsAsWarnings = doc.CreateElement("TreatTestAdapterErrorsAsWarnings");
            treatTestAdapterErrorsAsWarnings.InnerXml = this.TreatTestAdapterErrorsAsWarnings.ToString();
            root.AppendChild(treatTestAdapterErrorsAsWarnings);

            if (this.BinariesRoot != null)
            {
                XmlElement binariesRoot = doc.CreateElement("BinariesRoot");
                binariesRoot.InnerXml = this.BinariesRoot;
                root.AppendChild(binariesRoot);
            }

            if(!string.IsNullOrEmpty(this.TargetDevice))
            {
                XmlElement targetDevice = doc.CreateElement("TargetDevice");
                targetDevice.InnerXml = this.TargetDevice;
                root.AppendChild(targetDevice);
            }

            if (!string.IsNullOrEmpty(this.TestCaseFilter))
            {
                XmlElement testCaseFilter = doc.CreateElement(nameof(TestCaseFilter));
                testCaseFilter.InnerXml = this.TestCaseFilter;
                root.AppendChild(testCaseFilter);
            }
            
            if (!string.IsNullOrEmpty(this.DotnetHostPath))
            {
                XmlElement dotnetHostPath = doc.CreateElement(nameof(DotnetHostPath));
                dotnetHostPath.InnerXml = this.DotnetHostPath;
                root.AppendChild(dotnetHostPath);
            }

            if (this.TreatNoTestsAsError)
            {
                XmlElement treatAsError = doc.CreateElement(nameof(TreatNoTestsAsError));
                treatAsError.InnerText = this.TreatNoTestsAsError.ToString();
                root.AppendChild(treatAsError);
            }

            return root;
        }
#endif

        /// <summary>
        /// Loads RunConfiguration from XmlReader.
        /// </summary>
        /// <param name="reader">XmlReader having run configuration node.</param>
        /// <returns></returns>
        public static RunConfiguration FromXml(XmlReader reader)
        {
            ValidateArg.NotNull<XmlReader>(reader, nameof(reader));
            var runConfiguration = new RunConfiguration();
            var empty = reader.IsEmptyElement;

            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);

            // Process the fields in Xml elements
            reader.Read();
            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element)
                {
                    string elementName = reader.Name;
                    switch (elementName)
                    {
                        case "ResultsDirectory":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);

                            string resultsDir = reader.ReadElementContentAsString();
                            if (string.IsNullOrEmpty(resultsDir))
                            {
                                throw new SettingsException(
                                   string.Format(
                                       CultureInfo.CurrentCulture,
                                       Resources.Resources.InvalidSettingsIncorrectValue,
                                       Constants.RunConfigurationSettingsName,
                                       resultsDir,
                                       elementName));
                            }

                            runConfiguration.ResultsDirectory = resultsDir;
                            break;

                        case "CollectSourceInformation":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);
                            string collectSourceInformationStr = reader.ReadElementContentAsString();

                            bool bCollectSourceInformation = true;
                            if (!bool.TryParse(collectSourceInformationStr, out bCollectSourceInformation))
                            {
                                throw new SettingsException(String.Format(CultureInfo.CurrentCulture,
                                    Resources.Resources.InvalidSettingsIncorrectValue, Constants.RunConfigurationSettingsName, bCollectSourceInformation, elementName));
                            }

                            runConfiguration.ShouldCollectSourceInformation = bCollectSourceInformation;
                            break;

                        case "MaxCpuCount":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);

                            string cpuCount = reader.ReadElementContentAsString();
                            int count;
                            if (!int.TryParse(cpuCount, out count) || count < 0)
                            {
                                throw new SettingsException(
                                    string.Format(
                                        CultureInfo.CurrentCulture,
                                        Resources.Resources.InvalidSettingsIncorrectValue,
                                        Constants.RunConfigurationSettingsName,
                                        cpuCount,
                                        elementName));
                            }

                            runConfiguration.MaxCpuCount = count;
                            break;

                        case "BatchSize":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);

                            string batchSize = reader.ReadElementContentAsString();
                            long size;
                            if (!long.TryParse(batchSize, out size) || size < 0)
                            {
                                throw new SettingsException(
                                    string.Format(
                                        CultureInfo.CurrentCulture,
                                        Resources.Resources.InvalidSettingsIncorrectValue,
                                        Constants.RunConfigurationSettingsName,
                                        batchSize,
                                        elementName));
                            }

                            runConfiguration.BatchSize = size;
                            break;

                        case "TestSessionTimeout":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);

                            string testSessionTimeout = reader.ReadElementContentAsString();
                            long sessionTimeout;
                            if (!long.TryParse(testSessionTimeout, out sessionTimeout) || sessionTimeout < 0)
                            {
                                throw new SettingsException(
                                    string.Format(
                                        CultureInfo.CurrentCulture,
                                        Resources.Resources.InvalidSettingsIncorrectValue,
                                        Constants.RunConfigurationSettingsName,
                                        testSessionTimeout,
                                        elementName));
                            }

                            runConfiguration.TestSessionTimeout = sessionTimeout;
                            break;

                        case "DesignMode":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);

                            string designModeValueString = reader.ReadElementContentAsString();
                            bool designMode;
                            if (!bool.TryParse(designModeValueString, out designMode))
                            {
                                throw new SettingsException(String.Format(CultureInfo.CurrentCulture,
                                    Resources.Resources.InvalidSettingsIncorrectValue, Constants.RunConfigurationSettingsName, designModeValueString, elementName));
                            }
                            runConfiguration.DesignMode = designMode;
                            break;

                        case "InIsolation":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);

                            string inIsolationValueString = reader.ReadElementContentAsString();
                            bool inIsolation;
                            if (!bool.TryParse(inIsolationValueString, out inIsolation))
                            {
                                throw new SettingsException(String.Format(CultureInfo.CurrentCulture,
                                    Resources.Resources.InvalidSettingsIncorrectValue, Constants.RunConfigurationSettingsName, inIsolationValueString, elementName));
                            }
                            runConfiguration.InIsolation = inIsolation;
                            break;

                        case "DisableAppDomain":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);

                            string disableAppDomainValueString = reader.ReadElementContentAsString();
                            bool disableAppDomainCheck;
                            if (!bool.TryParse(disableAppDomainValueString, out disableAppDomainCheck))
                            {
                                throw new SettingsException(String.Format(CultureInfo.CurrentCulture,
                                    Resources.Resources.InvalidSettingsIncorrectValue, Constants.RunConfigurationSettingsName, disableAppDomainValueString, elementName));
                            }
                            runConfiguration.DisableAppDomain = disableAppDomainCheck;
                            break;

                        case "DisableParallelization":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);

                            string disableParallelizationValueString = reader.ReadElementContentAsString();
                            bool disableParallelizationCheck;
                            if (!bool.TryParse(disableParallelizationValueString, out disableParallelizationCheck))
                            {
                                throw new SettingsException(string.Format(CultureInfo.CurrentCulture,
                                    Resources.Resources.InvalidSettingsIncorrectValue, Constants.RunConfigurationSettingsName, disableParallelizationValueString, elementName));
                            }
                            runConfiguration.DisableParallelization = disableParallelizationCheck;
                            break;

                        case "TargetPlatform":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);
                            Architecture archType;
                            string value = reader.ReadElementContentAsString();
                            try
                            {
                                archType = (Architecture)Enum.Parse(typeof(Architecture), value, true);
                                if (archType != Architecture.X64 && archType != Architecture.X86 && archType != Architecture.ARM)
                                {
                                    throw new SettingsException(
                                        string.Format(
                                            CultureInfo.CurrentCulture,
                                            Resources.Resources.InvalidSettingsIncorrectValue,
                                            Constants.RunConfigurationSettingsName,
                                            value,
                                            elementName));
                                }
                            }
                            catch (ArgumentException)
                            {
                                throw new SettingsException(string.Format(CultureInfo.CurrentCulture,
                                    Resources.Resources.InvalidSettingsIncorrectValue, Constants.RunConfigurationSettingsName, value, elementName));
                            }

                            runConfiguration.TargetPlatform = archType;
                            break;

                        case "TargetFrameworkVersion":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);
                            Framework frameworkType;
                            value = reader.ReadElementContentAsString();
                            try
                            {
                                frameworkType = Framework.FromString(value);

                                if (frameworkType == null)
                                {
                                    throw new SettingsException(
                                        string.Format(
                                            CultureInfo.CurrentCulture,
                                            Resources.Resources.InvalidSettingsIncorrectValue,
                                            Constants.RunConfigurationSettingsName,
                                            value,
                                            elementName));
                                }
                            }
                            catch (ArgumentException)
                            {
                                throw new SettingsException(string.Format(CultureInfo.CurrentCulture,
                                    Resources.Resources.InvalidSettingsIncorrectValue, Constants.RunConfigurationSettingsName, value, elementName));
                            }

                            runConfiguration.TargetFramework = frameworkType;
                            break;

                        case "TestAdaptersPaths":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);
                            runConfiguration.TestAdaptersPaths = reader.ReadElementContentAsString();
                            break;

                        case "TreatTestAdapterErrorsAsWarnings":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);
                            bool treatTestAdapterErrorsAsWarnings = false;

                            value = reader.ReadElementContentAsString();

                            try
                            {
                                treatTestAdapterErrorsAsWarnings = bool.Parse(value);
                            }
                            catch (ArgumentException)
                            {
                                throw new SettingsException(string.Format(CultureInfo.CurrentCulture,
                                    Resources.Resources.InvalidSettingsIncorrectValue, Constants.RunConfigurationSettingsName, value, elementName));
                            }
                            catch (FormatException)
                            {
                                throw new SettingsException(string.Format(CultureInfo.CurrentCulture,
                                    Resources.Resources.InvalidSettingsIncorrectValue, Constants.RunConfigurationSettingsName, value, elementName));
                            }

                            runConfiguration.TreatTestAdapterErrorsAsWarnings = treatTestAdapterErrorsAsWarnings;
                            break;

                        case "SolutionDirectory":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);
                            string solutionDirectory = reader.ReadElementContentAsString();

#if !NETSTANDARD1_0
                            solutionDirectory = Environment.ExpandEnvironmentVariables(solutionDirectory);
#endif

                            if (string.IsNullOrEmpty(solutionDirectory)
#if !NETSTANDARD1_0
                                || !Directory.Exists(solutionDirectory)
#endif
                            )
                            {
                                if (EqtTrace.IsErrorEnabled)
                                {
                                    EqtTrace.Error(string.Format(CultureInfo.CurrentCulture, Resources.Resources.SolutionDirectoryNotExists, solutionDirectory));
                                }

                                solutionDirectory = null;
                            }

                            runConfiguration.SolutionDirectory = solutionDirectory;

                            break;

                        case "BinariesRoot":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);
                            runConfiguration.BinariesRoot = reader.ReadElementContentAsString();
                            break;

                        case "ExecutionThreadApartmentState":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);
                            string executionThreadApartmentState = reader.ReadElementContentAsString();
                            PlatformApartmentState apartmentState;
                            if (!Enum.TryParse(executionThreadApartmentState, out apartmentState))
                            {
                                throw new SettingsException(
                                    string.Format(
                                        CultureInfo.CurrentCulture,
                                        Resources.Resources.InvalidSettingsIncorrectValue,
                                        Constants.RunConfigurationSettingsName,
                                        executionThreadApartmentState,
                                        elementName));
                            }

                            runConfiguration.ExecutionThreadApartmentState = apartmentState;
                            break;

                        case "TargetDevice":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);
                            runConfiguration.TargetDevice = reader.ReadElementContentAsString();
                            break;

                        case "TestCaseFilter":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);
                            runConfiguration.TestCaseFilter = reader.ReadElementContentAsString();
                            break;

                        case "DotNetHostPath":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);
                            runConfiguration.DotnetHostPath = reader.ReadElementContentAsString();
                            break;
                        case "TreatNoTestsAsError":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);
                            string treatNoTestsAsErrorValueString = reader.ReadElementContentAsString();
                            bool treatNoTestsAsError;
                            if (!bool.TryParse(treatNoTestsAsErrorValueString, out treatNoTestsAsError))
                            {
                                throw new SettingsException(string.Format(CultureInfo.CurrentCulture,
                                    Resources.Resources.InvalidSettingsIncorrectValue, Constants.RunConfigurationSettingsName, treatNoTestsAsErrorValueString, elementName));
                            }
                            runConfiguration.TreatNoTestsAsError = treatNoTestsAsError;
                            break;

                        default:
                            // Ignore a runsettings element that we don't understand. It could occur in the case
                            // the test runner is of a newer version, but the test host is of an earlier version.
                            if (EqtTrace.IsErrorEnabled)
                            {
                                EqtTrace.Warning(
                                    string.Format(
                                        CultureInfo.CurrentCulture,
                                        Resources.Resources.InvalidSettingsXmlElement,
                                        Constants.RunConfigurationSettingsName,
                                        reader.Name));
                            }
                            reader.Skip();
                            break;
                    }
                }

                reader.ReadEndElement();
            }

            return runConfiguration;
        }
    }
}
