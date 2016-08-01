// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework
{
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using CommonResources = Microsoft.VisualStudio.TestPlatform.Common.Resources;

    /// <summary>
    /// Generic base class for managing extensions and looking them up by their URI. 
    /// </summary>
    /// <typeparam name="TExtension">The type of the extension.</typeparam>
    /// <typeparam name="TMetadata">The type of the metadata.</typeparam>
    internal abstract class TestExtensionManager<TExtension, TMetadata>
        where TMetadata : ITestExtensionCapabilities
    {
        #region Fields

        /// <summary>
        /// Used for logging errors.
        /// </summary>
        private IMessageLogger logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="unfilteredTestExtensions">
        /// The unfiltered Test Extensions.
        /// </param>
        /// <param name="testExtensions">
        /// The test Extensions.
        /// </param>
        /// <param name="logger">
        /// The logger.
        /// </param>
        protected TestExtensionManager(
            IEnumerable<LazyExtension<TExtension, Dictionary<string, object>>> unfilteredTestExtensions,
            IEnumerable<LazyExtension<TExtension, TMetadata>> testExtensions,
            IMessageLogger logger)
        {
            ValidateArg.NotNull<IEnumerable<LazyExtension<TExtension, Dictionary<string, object>>>>(unfilteredTestExtensions, "unfilteredTestExtensions");
            ValidateArg.NotNull<IEnumerable<LazyExtension<TExtension, TMetadata>>>(testExtensions, "testExtensions");
            ValidateArg.NotNull<IMessageLogger>(logger, "logger");

            this.logger = logger;
            this.TestExtensions = testExtensions;
            this.UnfilteredTestExtensions = unfilteredTestExtensions;

            // Populate the map to avoid threading issues
            this.PopulateMap();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets unfiltered list of test extensions which are available.
        /// </summary>
        /// <remarks>
        /// When we populate the "TestExtensions" property it
        /// will filter out extensions which are missing required pieces of metadata such
        /// as the "ExtensionUri".  This field is here so we can report on extensions which
        /// are missing metadata.
        /// </remarks>
        public IEnumerable<LazyExtension<TExtension, Dictionary<string, object>>> UnfilteredTestExtensions
        {
            get; private set;
        }

        /// <summary>
        /// Gets filtered list of test extensions which are available.
        /// </summary>
        /// <remarks>
        /// When we populate the "TestExtensions" property it
        /// will filter out extensions which are missing required pieces of metadata such
        /// as the "ExtensionUri".  This field is here so we can report on extensions which
        /// are missing metadata.
        /// </remarks>
        public IEnumerable<LazyExtension<TExtension, TMetadata>> TestExtensions
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets mapping between test extension URI and test extension.
        /// </summary>
        public Dictionary<Uri, LazyExtension<TExtension, TMetadata>> TestExtensionByUri
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Looks up the test extension by its URI.
        /// </summary>
        /// <param name="extensionUri">The URI of the test extension to be looked up.</param>
        /// <returns>The test extension or null if one was not found.</returns>
        public LazyExtension<TExtension, TMetadata> TryGetTestExtension(Uri extensionUri)
        {
            ValidateArg.NotNull<Uri>(extensionUri, "extensionUri");

            LazyExtension<TExtension, TMetadata> testExtension;
            this.TestExtensionByUri.TryGetValue(extensionUri, out testExtension);

            return testExtension;
        }

        /// <summary>
        /// Looks up the test extension by its URI (passed as a string).
        /// </summary>
        /// <param name="extensionUri">The URI of the test extension to be looked up.</param>
        /// <returns>The test extension or null if one was not found.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1057:StringUriOverloadsCallSystemUriOverloads", Justification = "Case insensitiveness needs to be supported.")]
        public LazyExtension<TExtension, TMetadata> TryGetTestExtension(string extensionUri)
        {
            ValidateArg.NotNull<string>(extensionUri, "extensionUri");

            LazyExtension<TExtension, TMetadata> testExtension = null;
            foreach (var availableExtensionUri in this.TestExtensionByUri.Keys)
            {
                if (string.Compare(extensionUri, availableExtensionUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    this.TestExtensionByUri.TryGetValue(availableExtensionUri, out testExtension);
                    break;
                }
            }
            return testExtension;
        }

        #endregion
        
        /// <summary>
        /// Populate the extension map.
        /// </summary>
        private void PopulateMap()
        {
            this.TestExtensionByUri = new Dictionary<Uri, LazyExtension<TExtension, TMetadata>>();

            if (this.TestExtensions == null)
            {
                return;
            }

            foreach (var extension in this.TestExtensions)
            {
                // Convert the extension uri string to an actual uri.
                Uri uri = null;
                try
                {
                    uri = new Uri(extension.Metadata.ExtensionUri);
                }
                catch (FormatException e)
                {
                    if (this.logger != null)
                    {
                        this.logger.SendMessage(
                            TestMessageLevel.Warning,
                            string.Format(CultureInfo.CurrentUICulture, CommonResources.InvalidExtensionUriFormat, extension.Metadata.ExtensionUri, e));
                    }
                }

                if (uri != null)
                {
                    // Make sure we are not trying to add an extension with a duplicate uri.
                    if (!this.TestExtensionByUri.ContainsKey(uri))
                    {
                        this.TestExtensionByUri.Add(uri, extension);
                    }
                    else
                    {
                        if (this.logger != null)
                        {
                            this.logger.SendMessage(
                                TestMessageLevel.Warning,
                                string.Format(CultureInfo.CurrentUICulture, CommonResources.DuplicateExtensionUri, extension.Metadata.ExtensionUri));
                        }
                    }
                }
            }
        }
    }
}
