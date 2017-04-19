// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// Manages loading and provides access to data collector extensions implementing the
    /// DataCollector interface.
    /// </summary>
    internal class DataCollectorExtensionManager : TestExtensionManager<DataCollector, IDataCollectorCapabilities>
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="unfilteredTestExtensions">
        /// The unfiltered Test Extensions.
        /// </param>
        /// <param name="testExtensions">
        /// The test Extensions.
        /// </param>
        /// <param name="logger">
        /// The logger.
        /// </param>
        /// <remarks>
        /// The constructor is not public because the factory method should be used to get instances of this class.
        /// </remarks>
        protected DataCollectorExtensionManager(
            IEnumerable<LazyExtension<DataCollector, Dictionary<string, object>>> unfilteredTestExtensions,
            IEnumerable<LazyExtension<DataCollector, IDataCollectorCapabilities>> testExtensions,
            IMessageLogger logger)
            : base(unfilteredTestExtensions, testExtensions, logger)
        {
        }

        /// <summary>
        /// Gets an instance of the DataCollectorExtensionManager.
        /// </summary>
        /// <param name="messageLogger">
        /// The message Logger.
        /// </param>
        /// <returns>
        /// The DataCollectorExtensionManager.
        /// </returns>
        public static DataCollectorExtensionManager Create(IMessageLogger messageLogger)
        {
            IEnumerable<LazyExtension<DataCollector, IDataCollectorCapabilities>> filteredTestExtensions;
            IEnumerable<LazyExtension<DataCollector, Dictionary<string, object>>> unfilteredTestExtensions;

            TestPluginManager.Instance.GetSpecificTestExtensions<DataCollectorConfig, DataCollector, IDataCollectorCapabilities, DataCollectorMetadata>(
                TestPlatformConstants.DataCollectorRegexPattern,
                out unfilteredTestExtensions,
                out filteredTestExtensions);

            return new DataCollectorExtensionManager(unfilteredTestExtensions, filteredTestExtensions, messageLogger);
        }
    }

    /// <summary>
    /// Hold data about the Data Collector.
    /// </summary>
    public class DataCollectorMetadata : IDataCollectorCapabilities
    {
        /// <summary>
        /// Constructor for DataCollectorMetadata
        /// </summary>
        /// <param name="extension">
        /// Uri identifying the data collector. 
        /// </param>
        /// <param name="friendlyName">
        /// The friendly Name.
        /// </param>
        public DataCollectorMetadata(string extension, string friendlyName)
        {
            this.ExtensionUri = extension;
            this.FriendlyName = friendlyName;
        }

        /// <summary>
        /// Gets Uri identifying the data collector.
        /// </summary>
        public string ExtensionUri
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets Friendly Name identifying the data collector.
        /// </summary>
        public string FriendlyName
        {
            get;
            private set;
        }
    }
}
