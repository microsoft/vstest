// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities
{
    using System;
    using System.Collections.Generic;

    public abstract class TestPluginInformation
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="testExtensionType">Data type of the test plugin</param>
        protected TestPluginInformation(Type testExtensionType)
        {
            if (testExtensionType != null)
            {
                this.AssemblyQualifiedName = testExtensionType.AssemblyQualifiedName;
            }
        }

        /// <summary>
        /// Gets data value identifying the test plugin
        /// </summary>
        public virtual string IdentifierData
        {
            get
            {
                return this.AssemblyQualifiedName;
            }
        }

        /// <summary>
        /// Metadata for the test plugin
        /// </summary>
        public virtual ICollection<Object> Metadata
        {
            get
            {
                return new object[] { this.AssemblyQualifiedName };
            }
        }

        /// <summary>
        /// Gets the Assembly qualified name of the plugin
        /// </summary>
        public string AssemblyQualifiedName
        {
            get;
            private set;
        }
    }
}
