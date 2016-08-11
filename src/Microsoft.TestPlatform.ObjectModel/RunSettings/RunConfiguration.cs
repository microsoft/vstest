// Copyright(c) Microsoft.All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    /// <summary>
    /// Stores information about a test settings.
    /// </summary>
    public class RunConfiguration : TestRunSettings
    {
        #region private fields

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
        private FrameworkVersion framework;

        /// <summary>
        /// Directory in which rocksteady/adapter should keep their run specific data. 
        /// </summary>
        private string resultsDirectory;

        /// <summary>
        /// Paths at which rocksteady should look for test adapters
        /// </summary>
        private string testAdaptersPaths;

        /// <summary>
        /// Inidication to adapters to disable app domain.
        /// </summary>
        private bool disableAppDomain;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes with the name of the test case.
        /// </summary>
        /// <param name="name">The name of the test case.</param>
        /// <param name="executorUri">The Uri of the executor to use for running this test.</param>
        public RunConfiguration()
            : base(Constants.RunConfigurationSettingsName)
        {
            // set defaults for target platform, framework version type and results directory.
            this.platform = Constants.DefaultPlatform;
            this.framework = Constants.DefaultFramework;
            this.resultsDirectory = Constants.DefaultResultsDirectory;
            this.SolutionDirectory = null;
            this.TreatTestAdapterErrorsAsWarnings = Constants.DefaultTreatTestAdapterErrorsAsWarnings;
            this.BinariesRoot = null;
            this.testAdaptersPaths = null;
            this.maxCpuCount = Constants.DefaultCpuCount;
            this.disableAppDomain = false;
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
        /// Disable App domain creation. 
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
        /// Gets or sets the Target platform this run is targeting. Possible values are x86|x64|arm|anycpu
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
        /// Gets or sets the target Framework this run is targeting. Possible values are Framework3.5|Framework4.0|Framework4.5
        /// </summary>
        public FrameworkVersion TargetFrameworkVersion
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
        /// Gets or sets the paths at which rocksteady should look for test adapters
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
        /// Gets a value indicating whether max cpu count set.
        /// </summary>
        public bool MaxCpuCountSet
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether app domain needs to be disabled by the adapters.
        /// </summary>
        public bool DisableAppDomainSet
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
        /// Gets the binaries root.
        /// </summary>
        public string BinariesRoot { get; private set; }

        #endregion
        
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

            XmlElement disableAppDomain = doc.CreateElement("DisableAppDomain");
            disableAppDomain.InnerXml = this.DisableAppDomain.ToString();
            root.AppendChild(disableAppDomain);

            XmlElement targetFrameworkVersion = doc.CreateElement("TargetFrameworkVersion");
            targetFrameworkVersion.InnerXml = this.TargetFrameworkVersion.ToString();
            root.AppendChild(targetFrameworkVersion);

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

            return root;
        }
        
        /// <summary>
        /// Loads RunConfiguration from XmlReader.
        /// </summary>
        /// <param name="reader">XmlReader having run configuration node.</param>
        /// <returns></returns>
        public static RunConfiguration FromXml(XmlReader reader)
        {
            ValidateArg.NotNull<XmlReader>(reader, "reader");
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
                            runConfiguration.ResultsDirectory = reader.ReadElementContentAsString();
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
                                        Resources.InvalidSettingsIncorrectValue,
                                        Constants.RunConfigurationSettingsName,
                                        cpuCount,
                                        elementName));
                            }

                            runConfiguration.MaxCpuCount = count;
                            break;

                        case "DisableAppDomain":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);

                            string appContainerCheck = reader.ReadElementContentAsString();
                            bool disableAppDomainCheck;
                            if (!bool.TryParse(appContainerCheck, out disableAppDomainCheck))
                            {
                                throw new SettingsException(String.Format(CultureInfo.CurrentCulture,
                                    Resources.InvalidSettingsIncorrectValue, Constants.RunConfigurationSettingsName, appContainerCheck, elementName));
                            }
                            runConfiguration.DisableAppDomain = disableAppDomainCheck;
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
                                            Resources.InvalidSettingsIncorrectValue,
                                            Constants.RunConfigurationSettingsName,
                                            value,
                                            elementName));
                                }
                            }
                            catch (ArgumentException)
                            {
                                throw new SettingsException(string.Format(CultureInfo.CurrentCulture,
                                    Resources.InvalidSettingsIncorrectValue, Constants.RunConfigurationSettingsName, value, elementName));
                            }

                            runConfiguration.TargetPlatform = archType;
                            break;

                        case "TargetFrameworkVersion":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);
                            FrameworkVersion frameworkType;
                            value = reader.ReadElementContentAsString();
                            try
                            {
                                frameworkType = (FrameworkVersion)Enum.Parse(typeof(FrameworkVersion), value, true);
                                if (frameworkType != FrameworkVersion.Framework35 && frameworkType != FrameworkVersion.Framework40 &&
                                    frameworkType != FrameworkVersion.Framework45)
                                {
                                    throw new SettingsException(
                                        string.Format(
                                            CultureInfo.CurrentCulture,
                                            Resources.InvalidSettingsIncorrectValue,
                                            Constants.RunConfigurationSettingsName,
                                            value,
                                            elementName));
                                }
                            }
                            catch (ArgumentException)
                            {
                                throw new SettingsException(string.Format(CultureInfo.CurrentCulture,
                                    Resources.InvalidSettingsIncorrectValue, Constants.RunConfigurationSettingsName, value, elementName));
                            }

                            runConfiguration.TargetFrameworkVersion = frameworkType;
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
                                    Resources.InvalidSettingsIncorrectValue, Constants.RunConfigurationSettingsName, value, elementName));
                            }
                            catch (FormatException)
                            {
                                throw new SettingsException(string.Format(CultureInfo.CurrentCulture,
                                    Resources.InvalidSettingsIncorrectValue, Constants.RunConfigurationSettingsName, value, elementName));
                            }

                            runConfiguration.TreatTestAdapterErrorsAsWarnings = treatTestAdapterErrorsAsWarnings;
                            break;

                        case "SolutionDirectory":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);
                            string solutionDirectory = reader.ReadElementContentAsString();
                            solutionDirectory = Environment.ExpandEnvironmentVariables(solutionDirectory);
                            if (string.IsNullOrEmpty(solutionDirectory) || !Directory.Exists(solutionDirectory))
                            {
                                if (EqtTrace.IsErrorEnabled)
                                {
                                    EqtTrace.Error(string.Format(CultureInfo.CurrentCulture, Resources.SolutionDirectoryNotExists, solutionDirectory));
                                }

                                solutionDirectory = null;
                            }

                            runConfiguration.SolutionDirectory = solutionDirectory;

                            break;

                        case "BinariesRoot":
                            XmlRunSettingsUtilities.ThrowOnHasAttributes(reader);
                            runConfiguration.BinariesRoot = reader.ReadElementContentAsString();
                            break;

                        default:
                            throw new SettingsException(
                                string.Format(
                                    CultureInfo.CurrentCulture,
                                    Resources.InvalidSettingsXmlElement,
                                    Constants.RunConfigurationSettingsName,
                                    reader.Name));
                    }
                }

                reader.ReadEndElement();
            }

            return runConfiguration;
        }
    }
}
