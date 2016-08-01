// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using ObjectModel;

    /// <summary>
    /// The test discoverer plugin information.
    /// </summary>
    internal class TestDiscovererPluginInformation : TestPluginInformation
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="testDiscovererType">Data type of the test discoverer</param>
        public TestDiscovererPluginInformation(Type testDiscovererType)
            : base(testDiscovererType)
        {
            if (testDiscovererType != null)
            {
                this.FileExtensions = GetFileExtensions(testDiscovererType);
                this.DefaultExecutorUri = GetDefaultExecutorUri(testDiscovererType);
            }
        }

        /// <summary>
        /// Metadata for the test plugin
        /// </summary>
        public override ICollection<Object> Metadata
        {
            get
            {
                return new object[] { this.FileExtensions, this.DefaultExecutorUri };
            }
        }

        /// <summary>
        /// Gets collection of file extensions supported by discoverer plugin.
        /// </summary>
        public List<string> FileExtensions
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the Uri identifying the executor
        /// </summary>
        public string DefaultExecutorUri
        {
            get;
            private set;
        }

        /// <summary>
        /// Helper to get file extensions from the FileExtensionAttribute on the discover plugin.
        /// </summary>
        /// <param name="testDicovererType">Data type of the test discoverer</param>
        /// <returns>List of file extensions</returns>
        private static List<string> GetFileExtensions(Type testDicovererType)
        {
            var fileExtensions = new List<string>();
            
            var attributes = testDicovererType.GetTypeInfo().GetCustomAttributes(typeof(FileExtensionAttribute), false).ToArray();
            if (attributes != null && attributes.Length > 0)
            {
                foreach (var attribute in attributes)
                {
                    var fileExtAttribute = (FileExtensionAttribute)attribute;
                    if (!string.IsNullOrEmpty(fileExtAttribute.FileExtension))
                    {
                        fileExtensions.Add(fileExtAttribute.FileExtension);
                    }
                }
            }

            return fileExtensions;
        }

        /// <summary>
        /// Returns the value of default executor Uri on this type. 'Null' if not present.
        /// </summary>
        /// <param name="testDiscovererType"> The test discoverer Type. </param>
        /// <returns> The default executor URI. </returns>
        private static string GetDefaultExecutorUri(Type testDiscovererType)
        {
            string result = string.Empty;
            
            object[] attributes = testDiscovererType.GetTypeInfo().GetCustomAttributes(typeof(DefaultExecutorUriAttribute), false).ToArray();
            if (attributes != null && attributes.Length > 0)
            {
                DefaultExecutorUriAttribute executorUriAttribute = (DefaultExecutorUriAttribute)attributes[0];

                if (!string.IsNullOrEmpty(executorUriAttribute.ExecutorUri))
                {
                    result = executorUriAttribute.ExecutorUri;
                }
            }

            return result;
        }
    }
}
