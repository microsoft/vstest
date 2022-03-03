// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

/// <summary>
/// Creates and return a list of available attachments processor
/// </summary>
internal interface IDataCollectorAttachmentsProcessorsFactory
{
    /// <summary>
    /// Creates and return a list of available attachments processor
    /// </summary>
    /// <param name="invokedDataCollector">List of invoked data collectors</param>
    /// <param name="logger">Message logger</param>
    /// <returns>List of attachments processors</returns>
    DataCollectorAttachmentProcessor[] Create(InvokedDataCollector[] invokedDataCollectors, IMessageLogger logger);
}

/// <summary>
/// Registered data collector attachment processor
/// </summary>
internal class DataCollectorAttachmentProcessor
{
    /// <summary>
    /// Data collector FriendlyName
    /// </summary>
    public string FriendlyName { get; private set; }

    /// <summary>
    /// Data collector attachment processor instance
    /// </summary>
    public IDataCollectorAttachmentProcessor DataCollectorAttachmentProcessorInstance { get; private set; }

    public DataCollectorAttachmentProcessor(string friendlyName, IDataCollectorAttachmentProcessor dataCollectorAttachmentProcessor!!)
    {
        FriendlyName = string.IsNullOrEmpty(friendlyName) ? throw new ArgumentException("Invalid FriendlyName", nameof(friendlyName)) : friendlyName;
        DataCollectorAttachmentProcessorInstance = dataCollectorAttachmentProcessor;
    }
}
