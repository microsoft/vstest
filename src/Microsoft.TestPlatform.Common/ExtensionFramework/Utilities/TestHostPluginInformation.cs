// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using ObjectModel;

    /// <summary>
    /// The test host plugin information.
    /// </summary>
    internal class TestHostPluginInformation : TestExtensionPluginInformation
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="testHostType"> The testhost Type. </param>
        public TestHostPluginInformation(Type testHostType)
            : base(testHostType)
        {
            this.FriendlyName = GetFriendlyName(testHostType);
        }

        /// <summary>
        /// Gets the Friendly Name identifying the testhost
        /// </summary>
        public string FriendlyName
        {
            get;
            private set;
        }

        /// <summary>
        /// Metadata for the testhost plugin
        /// </summary>
        public override ICollection<Object> Metadata
        {
            get
            {
                return new Object[] { this.ExtensionUri, this.FriendlyName };
            }
        }

        /// <summary>
        /// Helper to get the FriendlyName from the FriendlyNameAttribute on testhost plugin.
        /// </summary>
        /// <param name="testHostType">Data type of the testhost</param>
        /// <returns>FriendlyName identifying the testhost</returns>
        private static string GetFriendlyName(Type testHostType)
        {
            string friendlyName = string.Empty;

            object[] attributes = testHostType.GetTypeInfo().GetCustomAttributes(typeof(FriendlyNameAttribute), false).ToArray();
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
