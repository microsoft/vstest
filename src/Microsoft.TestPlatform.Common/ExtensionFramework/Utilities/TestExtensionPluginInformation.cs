// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// The test extension plugin information.
    /// </summary>
    internal abstract class TestExtensionPluginInformation : TestPluginInformation
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="type"> The test Logger Type. </param>
        public TestExtensionPluginInformation(Type type)
            : base(type)
        {
            if (type != null)
            {
                this.ExtensionUri = GetExtensionUri(type);
            }
        }

        /// <summary>
        /// Gets data value identifying the test plugin
        /// </summary>
        public override string IdentifierData
        {
            get
            {
                return this.ExtensionUri;
            }
        }

        /// <summary>
        /// Metadata for the test plugin
        /// </summary>
        public override ICollection<object> Metadata
        {
            get
            {
                return new object[] { this.ExtensionUri };
            }
        }

        /// <summary>
        /// Gets the Uri identifying the test extension.
        /// </summary>
        public string ExtensionUri
        {
            get;
            private set;
        }

        /// <summary>
        /// Helper to get the Uri from the ExtensionUriAttribute on logger plugin.
        /// </summary>
        /// <param name="testLoggerType">Data type of the test logger</param>
        /// <returns>Uri identifying the test logger</returns>
        private static string GetExtensionUri(Type testLoggerType)
        {
            string extensionUri = string.Empty;

            object[] attributes = testLoggerType.GetTypeInfo().GetCustomAttributes(typeof(ExtensionUriAttribute), false).ToArray();
            if (attributes != null && attributes.Length > 0)
            {
                ExtensionUriAttribute extensionUriAttribute = (ExtensionUriAttribute)attributes[0];

                if (!string.IsNullOrEmpty(extensionUriAttribute.ExtensionUri))
                {
                    extensionUri = extensionUriAttribute.ExtensionUri;
                }
            }

            if (EqtTrace.IsErrorEnabled && string.IsNullOrEmpty(extensionUri))
            {
                EqtTrace.Error("The type \"{0}\" defined in \"{1}\" does not have ExtensionUri attribute.", testLoggerType.ToString(), testLoggerType.GetTypeInfo().Module.Name);
            }

            return extensionUri;
        }
    }
}
