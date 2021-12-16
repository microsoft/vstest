// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine
{
    /// <summary>
    /// Orchestrates test run attachments processing operations.
    /// </summary>
    internal interface ITestRunAttachmentsProcessingManager
    {
        /// <summary>
        /// Processes attachments and provides results through handler
        /// </summary>
        /// <param name="runSettingsXml">RunSettings</param>
        /// <param name="attachments">Collection of attachments</param>
        /// <param name="invokedDataCollector">Collection of invoked data collectors</param>
        /// <param name="eventHandler">EventHandler for handling test run attachments processing event</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ProcessTestRunAttachmentsAsync(string runSettingsXml, IRequestData requestData, IEnumerable<AttachmentSet> attachments, IEnumerable<InvokedDataCollector> invokedDataCollector, ITestRunAttachmentsProcessingEventsHandler eventHandler, CancellationToken cancellationToken);

        /// <summary>
        /// Processes attachments
        /// </summary>
        /// <param name="runSettingsXml">RunSettings</param>
        /// <param name="attachments">Collection of attachments</param>
        /// <param name="invokedDataCollector">Collection of invoked data collectors</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of attachments.</returns>
        Task<Collection<AttachmentSet>> ProcessTestRunAttachmentsAsync(string runSettingsXml, IRequestData requestData, IEnumerable<AttachmentSet> attachments, IEnumerable<InvokedDataCollector> invokedDataCollector, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Creates and return a list of available attachments processor
    /// </summary>
    internal interface IDataCollectorAttachmentsProcessorsFactory
    {
        /// <summary>
        /// Creates and return a list of available attachments processor
        /// </summary>
        /// <param name="invokedDataCollector">List of invoked data collectors</param>
        /// <returns>List of attachments processors</returns>
        IReadOnlyDictionary<string, IDataCollectorAttachmentProcessor> Create(InvokedDataCollector[] invokedDataCollectors);
    }
}
