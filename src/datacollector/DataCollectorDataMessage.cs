// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// Base class for messages
    /// </summary>
    internal class DataCollectorDataMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectorDataMessage"/> class. 
        /// </summary>
        /// <param name="context">Data collection context.
        /// </param>
        /// <param name="uri">Uri of data collector
        /// </param>
        /// <param name="friendlyName">Friendly name of data collector
        /// </param>
        internal DataCollectorDataMessage(DataCollectionContext context, Uri uri, string friendlyName)
        {
            ValidateArg.NotNull(context, "context");
            ValidateArg.NotNull(uri, "uri");
            ValidateArg.NotNullOrEmpty(friendlyName, "friendlyName");

            this.DataCollectionContext = context;
            this.Uri = uri;
            this.FriendlyName = friendlyName;
        }

        /// <summary>
        /// Gets data collection context in which transfer is initiated.
        /// </summary>
        internal DataCollectionContext DataCollectionContext
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets Uri of data collector initiating the data transfer
        /// </summary>
        internal Uri Uri
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets friendly name of data collector initiating the data transfer.
        /// </summary>
        internal string FriendlyName
        {
            get;
            private set;
        }
    }
}