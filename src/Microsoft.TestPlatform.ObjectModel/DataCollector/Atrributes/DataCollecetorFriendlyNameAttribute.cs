// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.Atrributes
{
    using System;

    /// <summary>
    /// Provides a friendly name for the data collector.
    /// </summary>
    public class DataCollecetorFriendlyNameAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollecetorFriendlyNameAttribute"/> class.
        /// </summary>
        /// <param name="friendlyName">
        /// The friendly name.
        /// </param>
        public DataCollecetorFriendlyNameAttribute(string friendlyName)
        {
            this.FriendlyName = friendlyName;
        }

        /// <summary>
        /// Gets the friendly name.
        /// </summary>
        public string FriendlyName { get; private set; }
    }
}
