// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;

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

    private static readonly object Synclock = new();
    private TExtension _extension;
    private TMetadata _metadata;
    private readonly Type _metadataType;
    private readonly Func<TExtension> _extensionCreator;

    #endregion

    #region Constructors

    /// <summary>
    /// The constructor.
    /// </summary>
    /// <param name="instance">Test extension Instance</param>
    /// <param name="metadata">test extension metadata</param>
    public LazyExtension(TExtension instance!!, TMetadata metadata)
    {
        if (metadata == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        _extension = instance;
        _metadata = metadata;
        IsExtensionCreated = true;
    }

    /// <summary>
    /// The constructor.
    /// </summary>
    /// <param name="pluginInfo">Test plugin to instantiated on demand.</param>
    /// <param name="metadataType">Metadata type to instantiate on demand</param>
    public LazyExtension(TestPluginInformation pluginInfo!!, Type metadataType!!)
    {
        TestPluginInfo = pluginInfo;
        _metadataType = metadataType;
        IsExtensionCreated = false;
    }

    /// <summary>
    /// The constructor.
    /// </summary>
    /// <param name="pluginInfo">Test plugin to instantiated on demand</param>
    /// <param name="metadata">Test extension metadata</param>
    public LazyExtension(TestPluginInformation pluginInfo!!, TMetadata metadata!!)
    {
        TestPluginInfo = pluginInfo;
        _metadata = metadata;
        IsExtensionCreated = false;
    }

    /// <summary>
    /// Delegate Constructor
    /// </summary>
    /// <param name="creator">Test extension creator delegate</param>
    /// <param name="metadata">test extension metadata</param>
    public LazyExtension(Func<TExtension> creator!!, TMetadata metadata!!)
    {
        _extensionCreator = creator;
        _metadata = metadata;
        IsExtensionCreated = false;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets a value indicating whether is extension created.
    /// </summary>
    internal bool IsExtensionCreated { get; private set; }

    internal TestPluginInformation TestPluginInfo { get; }

    /// <summary>
    /// Gets the test extension instance.
    /// </summary>
    public TExtension Value
    {
        get
        {
            if (!IsExtensionCreated)
            {
                if (_extensionCreator != null)
                {
                    _extension = _extensionCreator();
                }
                else if (_extension == null)
                {
                    lock (Synclock)
                    {
                        if (_extension == null && TestPluginInfo != null)
                        {
                            var pluginType = TestPluginManager.GetTestExtensionType(TestPluginInfo.AssemblyQualifiedName);
                            _extension = TestPluginManager.CreateTestExtension<TExtension>(pluginType);
                        }
                    }
                }

                IsExtensionCreated = true;
            }

            return _extension;
        }
    }

    /// <summary>
    /// Gets the test extension metadata
    /// </summary>
    public TMetadata Metadata
    {
        get
        {
            if (_metadata == null)
            {
                lock (Synclock)
                {
                    if (_metadata == null && TestPluginInfo != null)
                    {
                        var parameters = TestPluginInfo.Metadata?.ToArray();
                        var dataObject = Activator.CreateInstance(_metadataType, parameters);
                        _metadata = (TMetadata)dataObject;
                    }
                }
            }

            return _metadata;
        }
    }

    #endregion
}
