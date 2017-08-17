// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

    /// <summary>
    /// Defines the defaults/constants used across different components.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The in process data collection run settings name.
        /// </summary>
        public const string InProcDataCollectionRunSettingsName = "InProcDataCollectionRunSettings";

        /// <summary>
        /// The in process data collector setting name.
        /// </summary>
        public const string InProcDataCollectorSettingName = "InProcDataCollector";

        /// <summary>
        /// The in process data collectors setting name.
        /// </summary>
        public const string InProcDataCollectorsSettingName = "InProcDataCollectors";

        /// <summary>
        /// Name of data collection settigns node in RunSettings.
        /// </summary>
        public const string DataCollectionRunSettingsName = "DataCollectionRunSettings";

        /// <summary>
        /// Name of RunConfiguration settings node in RunSettings.
        /// </summary>
        public const string RunConfigurationSettingsName = "RunConfiguration";

        /// <summary>
        /// Default testrunner if testrunner is not specified
        /// </summary>
        public const string UnspecifiedAdapterPath = "_none_";

        public const string DataCollectorsSettingName = "DataCollectors";

        public const string RunSettingsName = "RunSettings";

        public const string DataCollectorSettingName = "DataCollector";

        public const string TestRunParametersName = "TestRunParameters";

        /// <summary>
        /// Type of the unit test extension. (Extension author will use this name while authoring their Vsix)
        /// </summary>
        public const string UnitTestExtensionType = "UnitTestExtension";

        /// <summary>
        /// Maximum size of the trace log file (in kilobytes).
        /// </summary>
        public const string TraceLogMaxFileSizeInKB = "TraceLogMaxFileSizeInKb";

        public const string EmptyRunSettings = @"<RunSettings></RunSettings>";

        public static readonly Architecture DefaultPlatform = XmlRunSettingsUtilities.OSArchitecture == Architecture.ARM ? Architecture.ARM : Architecture.X86;

        /// <summary>
        /// Default option for parallel execution
        /// </summary>
        public const int DefaultCpuCount = 1;

        /// <summary>
        /// The default batch size.
        /// </summary>
        public const long DefaultBatchSize = 10;

        /// <summary>
        /// The default protocol version
        /// </summary>
        public static readonly ProtocolConfig DefaultProtocolConfig = new ProtocolConfig { Version = 2 };

        /// <summary>
        /// Name of the results directory
        /// </summary>
        public const string ResultsDirectoryName = "TestResults";

        /// <summary>
        /// Default results directory.
        /// </summary>
        public static readonly string DefaultResultsDirectory = Path.Combine(Directory.GetCurrentDirectory(), ResultsDirectoryName);

        /// <summary>
        /// Default treatment of error from test adapters.
        /// </summary>
        public const bool DefaultTreatTestAdapterErrorsAsWarnings = false;

        /// <summary>
        /// The default execution thread apartment state.
        /// </summary>
        [CLSCompliant(false)]
        public static readonly PlatformApartmentState DefaultExecutionThreadApartmentState = PlatformApartmentState.MTA;

        /// <summary>
        ///  Contants for detecting .net framework.
        /// </summary>
        public const string TargetFrameworkAttributeFullName = "System.Runtime.Versioning.TargetFrameworkAttribute";

        public const string DotNetFrameWorkStringPrefix = ".NETFramework,Version=";

        public const string DotNetFramework40 = ".NETFramework,Version=v4.0";

        public const string DotNetFramework45 = ".NETFramework,Version=v4.5";

        public const string DotNetFramework451 = ".NETFramework,Version=v4.5.1";

        public const string DotNetFramework46 = ".NETFramework,Version=v4.6";

        public const string TargetFrameworkName = "TargetFrameworkName";
    }

    /// <summary>
    /// Default parameters to be passed onto all loggers.
    /// </summary>
    public static class DefaultLoggerParameterNames
    {
        // Denotes target location for test run resutls
        // For ex. TrxLogger saves test run results at this target
        public const string TestRunDirectory = "TestRunDirectory";
    }

}
