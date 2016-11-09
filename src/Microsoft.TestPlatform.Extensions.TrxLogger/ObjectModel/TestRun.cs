// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Security.Principal;

    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    using TrxLoggerResources = Microsoft.TestPlatform.Extensions.TrxLogger.Resources.TrxResource;

    /// <summary>
    /// Class having information about a test run.
    /// </summary>
    public sealed class TestRun
    {
        #region Fields

        #region Summary fields

        // These fields will be valid when the test run summary is loaded from a results file.
        // The summary fields need to be first in the class so they get serialized first. When we
        // read the summary we don't want to parse the XML tags for other fields because they can
        // be quite large.
        //
        // When reading the results file, the summary is considered complete when all summary fields
        // are non-null. Any new summary fields that are initialized in the constructor should be
        // placed before the last non-initialized field.
        //
        // The summary parsing code is in XmlTestReader.ReadTestRunSummary.
        [StoreXmlSimpleField]
        private Guid id;

        [StoreXmlSimpleField]
        private string name;

        [StoreXmlSimpleField("@runUser", "")]
        private string runUser;

        private TestRunConfiguration runConfig;

        #endregion Summary fields

        #region Non-summary fields
        [StoreXmlSimpleField("Times/@creation")]
        private DateTime created;

        [StoreXmlSimpleField("Times/@queuing")]
        private DateTime queued;

        [StoreXmlSimpleField("Times/@start")]
        private DateTime started;

        [StoreXmlSimpleField("Times/@finish")]
        private DateTime finished;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRun"/> class.
        /// </summary>
        /// <param name="runId">
        /// The run id.
        /// </param>
        internal TestRun(Guid runId)
        {
            this.Initialize();

            EqtAssert.IsTrue(!Guid.Empty.Equals(runId), "Can't use Guid.Empty for run ID.");
            this.id = runId;
        }

        #endregion Constructors

        /// <summary>
        /// Gets or sets the run configuration.
        /// </summary>
        internal TestRunConfiguration RunConfiguration
        {
            get
            {
                return this.runConfig;
            }

            set
            {
                EqtAssert.ParameterNotNull(value, "RunConfiguration");
                this.runConfig = value;
            }
        }

        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        internal DateTime Started
        {
            get
            {
                return this.started;
            }

            set
            {
                this.started = value;
            }
        }

        /// <summary>
        /// Gets or sets the finished time of Test run.
        /// </summary>
        internal DateTime Finished
        {
            get { return this.finished; }
            set { this.finished = value; }
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        internal string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                EqtAssert.StringNotNullOrEmpty(value, "Name");
                this.name = value;
            }
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        internal Guid Id
        {
            get { return this.id; }
        }

        /// <summary>
        /// WARNING: do not use from inside Test Adapters, use from only on HA by UI etc.
        /// Returns directory on HA for dependent files for TestResult. XmlPersistence method for UI.
        /// Throws on error (e.g. if deployment directory was not set for test run).
        /// </summary>
        /// <param name="result">
        /// Test Result to get dependent files directory for.
        /// </param>
        /// <returns>
        /// Result directory.
        /// </returns>
        internal string GetResultFilesDirectory(UnitTestResult result)
        {
            EqtAssert.ParameterNotNull(result, "result");
            return Path.Combine(this.GetResultsDirectory(), result.RelativeTestResultsDirectory);
        }

        /// <summary>
        /// Gets the results directory, which is the run deployment In directory
        /// </summary>
        /// <returns>The results directory</returns>
        /// <remarks>This method is called by public properties/methods, so it needs to throw on error</remarks>
        internal string GetResultsDirectory()
        {
            if (this.RunConfiguration == null)
            {
                Debug.Fail("'RunConfiguration' is null");
                throw new Exception(String.Format(CultureInfo.CurrentCulture, TrxLoggerResources.Common_MissingRunConfigInRun));
            }

            if (string.IsNullOrEmpty(this.RunConfiguration.RunDeploymentRootDirectory))
            {
                Debug.Fail("'RunConfiguration.RunDeploymentRootDirectory' is null or empty");
                throw new Exception(String.Format(CultureInfo.CurrentCulture, TrxLoggerResources.Common_MissingRunDeploymentRootInRunConfig));
            }

            return this.RunConfiguration.RunDeploymentInDirectory;
        }

        private static string FormatDateTimeForRunName(DateTime timeStamp)
        {
            // We use custom format string to make sure that runs are sorted in the same way on all intl machines.
            // This is both for directory names and for Data Warehouse.
            return timeStamp.ToString("yyyy-MM-dd HH:mm:ss", DateTimeFormatInfo.InvariantInfo);
        }

        private void Initialize()
        {
            this.id = Guid.NewGuid();
            this.name = String.Format(CultureInfo.CurrentCulture, TrxLoggerResources.Common_TestRunName, Environment.GetEnvironmentVariable("UserName"), Environment.MachineName, FormatDateTimeForRunName(DateTime.Now));
            this.runUser = WindowsIdentity.GetCurrent().Name;
            this.created = DateTime.Now.ToUniversalTime();
            this.queued = DateTime.Now.ToUniversalTime();
            this.started = DateTime.Now.ToUniversalTime();
            this.finished = DateTime.Now.ToUniversalTime();
        }
    }
}
