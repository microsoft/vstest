// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;

    /// <summary>
    /// The data collector config.
    /// </summary>
    public class DataCollectorConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectorConfig"/> class.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        public DataCollectorConfig(Type type) : this(type, new DataCollectorLoader())
        {
        }

        internal DataCollectorConfig(Type type, IDataCollectorLoader dataCollectorLoader)
        {
            ValidateArg.NotNull(type, nameof(type));

            this.DataCollectorType = type;
            this.TypeUri = dataCollectorLoader.GetTypeUri(type);
            this.FriendlyName = dataCollectorLoader.GetFriendlyName(type);
        }

        /// <summary>
        /// Gets the data collector type.
        /// </summary>
        public Type DataCollectorType { get; private set; }

        /// <summary>
        /// Gets the type uri.
        /// </summary>
        public Uri TypeUri { get; private set; }

        /// <summary>
        /// Gets the friendly name.
        /// </summary>
        public string FriendlyName { get; private set; }
    }
}