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

        private static readonly object synclock = new();
        private TExtension extension;
        private TMetadata metadata;
        private readonly Type metadataType;
        private readonly Func<TExtension> extensionCreator;

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
                throw new ArgumentNullException(nameof(instance));
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            extension = instance;
            this.metadata = metadata;
            IsExtensionCreated = true;
        }

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="pluginInfo">Test plugin to instantiated on demand.</param>
        /// <param name="metadataType">Metadata type to instantiate on demand</param>
        public LazyExtension(TestPluginInformation pluginInfo, Type metadataType)
        {
            TestPluginInfo = pluginInfo ?? throw new ArgumentNullException(nameof(pluginInfo));
            this.metadataType = metadataType ?? throw new ArgumentNullException(nameof(metadataType));
            IsExtensionCreated = false;
        }

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="pluginInfo">Test plugin to instantiated on demand</param>
        /// <param name="metadata">Test extension metadata</param>
        public LazyExtension(TestPluginInformation pluginInfo, TMetadata metadata)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            TestPluginInfo = pluginInfo ?? throw new ArgumentNullException(nameof(pluginInfo));
            this.metadata = metadata;
            IsExtensionCreated = false;
        }

        /// <summary>
        /// Delegate Constructor
        /// </summary>
        /// <param name="creator">Test extension creator delegate</param>
        /// <param name="metadata">test extension metadata</param>
        public LazyExtension(Func<TExtension> creator, TMetadata metadata)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            extensionCreator = creator ?? throw new ArgumentNullException(nameof(creator));
            this.metadata = metadata;
            IsExtensionCreated = false;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets a value indicating whether is extension created.
        /// </summary>
        internal bool IsExtensionCreated { get; private set; }

        internal TestPluginInformation TestPluginInfo { get; private set; }

        /// <summary>
        /// Gets the test extension instance.
        /// </summary>
        public TExtension Value
        {
            get
            {
                if (!IsExtensionCreated)
                {
                    if (extensionCreator != null)
                    {
                        extension = extensionCreator();
                    }
                    else if (extension == null)
                    {
                        lock (synclock)
                        {
                            if (extension == null && TestPluginInfo != null)
                            {
                                var pluginType = TestPluginManager.GetTestExtensionType(TestPluginInfo.AssemblyQualifiedName);
                                extension = TestPluginManager.CreateTestExtension<TExtension>(pluginType);
                            }
                        }
                    }

                    IsExtensionCreated = true;
                }

                return extension;
            }
        }

        /// <summary>
        /// Gets the test extension metadata
        /// </summary>
        public TMetadata Metadata
        {
            get
            {
                if (metadata == null)
                {
                    lock (synclock)
                    {
                        if (metadata == null && TestPluginInfo != null)
                        {
                            var parameters = TestPluginInfo.Metadata?.ToArray();
                            var dataObject = Activator.CreateInstance(metadataType, parameters);
                            metadata = (TMetadata)dataObject;
                        }
                    }
                }

                return metadata;
            }
        }

        #endregion
    }
}
