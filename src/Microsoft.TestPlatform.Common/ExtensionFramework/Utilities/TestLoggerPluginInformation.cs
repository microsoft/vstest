// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    using ObjectModel;

    /// <summary>
    /// The test logger plugin information.
    /// </summary>
    internal class TestLoggerPluginInformation : TestExtensionPluginInformation
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="testLoggerType"> The test Logger Type. </param>
        public TestLoggerPluginInformation(Type testLoggerType)
            : base(testLoggerType)
        {
            this.FriendlyName = GetFriendlyName(testLoggerType);
        }

        /// <summary>
        /// Gets the Friendly Name identifying the logger
        /// </summary>
        public string FriendlyName
        {
            get;
            private set;
        }

        /// <summary>
        /// Metadata for the test plugin
        /// </summary>
        public override ICollection<Object> Metadata
        {
            get
            {
                return new Object[] { this.ExtensionUri, this.FriendlyName };
            }
        }

        /// <summary>
        /// Helper to get the FriendlyName from the FriendlyNameAttribute on logger plugin.
        /// </summary>
        /// <param name="testLoggerType">Data type of the test logger</param>
        /// <returns>FriendlyName identifying the test logger</returns>
        private static string GetFriendlyName(Type testLoggerType)
        {
            string friendlyName = string.Empty;

            object[] attributes = testLoggerType.GetTypeInfo().GetCustomAttributes(typeof(FriendlyNameAttribute), false).ToArray();
            if (attributes != null && attributes.Length > 0)
            {
                FriendlyNameAttribute friendlyNameAttribute = (FriendlyNameAttribute)attributes[0];

                if (!string.IsNullOrEmpty(friendlyNameAttribute.FriendlyName))
                {
                    friendlyName = friendlyNameAttribute.FriendlyName;
                }
            }

            return friendlyName;
        }
    }
}
