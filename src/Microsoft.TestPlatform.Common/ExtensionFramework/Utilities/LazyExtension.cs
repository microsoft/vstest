// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities
{
    using System;
    using System.Linq;

    /// <summary>
    /// Class to hold a test extension type
    /// </summary>
    /// <typeparam name="TExtension">Test extension type</typeparam>
    /// <typeparam name="TMetadata">Test extension metadata</typeparam>
    public class LazyExtension<TExtension, TMetadata>
    {
        #region Private Members

        private static object synclock = new object();
        private TExtension extension;
        private TMetadata metadata;
        private TestPluginInformation testPluginInfo;
        private Type metadataType;
        private bool isExtensionCreated;
        private Func<TExtension> extensionCreator;

        #endregion

        #region Constructors

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="instance">Test extension Instance</param>
        /// <param name="metadata">test extension metadata</param>
        public LazyExtension(TExtension instance, TMetadata metadata)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance"); ;
            }

            if (metadata == null)
            {
                throw new ArgumentNullException("instance"); ;
            }

            this.extension = instance;
            this.metadata = metadata;
            this.isExtensionCreated = true;
        }

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="pluginInfo">Test plugin to instantiated on demand.</param>
        /// <param name="metadataType">Metadata type to instantiate on demand</param>
        public LazyExtension(TestPluginInformation pluginInfo, Type metadataType)
        {
            if (pluginInfo == null)
            {
                throw new ArgumentNullException("pluginInfo"); ;
            }

            if (metadataType == null)
            {
                throw new ArgumentNullException("metadataType"); ;
            }

            this.testPluginInfo = pluginInfo;
            this.metadataType = metadataType;
            this.isExtensionCreated = false;
        }

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="pluginInfo">Test plugin to instantiated on demand</param>
        /// <param name="metadata">Test extension metadata</param>
        public LazyExtension(TestPluginInformation pluginInfo, TMetadata metadata)
        {
            if (pluginInfo == null)
            {
                throw new ArgumentNullException("pluginInfo"); ;
            }

            if (metadata == null)
            {
                throw new ArgumentNullException("metadata"); ;
            }

            this.testPluginInfo = pluginInfo;
            this.metadata = metadata;
            this.isExtensionCreated = false;
        }

        /// <summary>
        /// Delegate Constructor
        /// </summary>
        /// <param name="creator">Test extension creator delegate</param>
        /// <param name="metadata">test extension metadata</param>
        public LazyExtension(Func<TExtension> creator, TMetadata metadata)
        {
            if (creator == null)
            {
                throw new ArgumentNullException("creator"); ;
            }

            if (metadata == null)
            {
                throw new ArgumentNullException("metadata"); ;
            }

            this.extensionCreator = creator;
            this.metadata = metadata;
            this.isExtensionCreated = false;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets a value indicating whether is extension created.
        /// </summary>
        internal bool IsExtensionCreated
        {
            get
            {
                return this.isExtensionCreated;
            }
        }

        internal TestPluginInformation TestPluginInfo => this.testPluginInfo;

        /// <summary>
        /// Gets the test extension instance.
        /// </summary>
        public TExtension Value
        {
            get
            {
                if (!this.isExtensionCreated)
                {
                    if (this.extensionCreator != null)
                    {
                        this.extension = this.extensionCreator();
                    }
                    else if (this.extension == null)
                    {
                        lock (synclock)
                        {
                            if (this.extension == null && this.testPluginInfo != null)
                            {
                                var pluginType = TestPluginManager.GetTestExtensionType(this.testPluginInfo.AssemblyQualifiedName);
                                this.extension = TestPluginManager.CreateTestExtension<TExtension>(pluginType);
                            }
                        }
                    }

                    this.isExtensionCreated = true;
                }

                return this.extension;
            }
        }

        /// <summary>
        /// Gets the test extension metadata
        /// </summary>
        public TMetadata Metadata
        {
            get
            {
                if (this.metadata == null)
                {
                    lock (synclock)
                    {
                        if (this.metadata == null && this.testPluginInfo != null)
                        {
                            var parameters = this.testPluginInfo.Metadata == null ? null : this.testPluginInfo.Metadata.ToArray();
                            var dataObject = Activator.CreateInstance(this.metadataType, parameters);
                            this.metadata = (TMetadata)dataObject;
                        }
                    }
                }

                return this.metadata;
            }
        }

        #endregion
    }
}
