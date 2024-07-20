﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

/// <summary>
/// Interface for data collectors add-ins that choose to reprocess generated attachments
/// </summary>
public interface IDataCollectorAttachmentProcessor
{
    /// <summary>
    /// Gets the attachments Uris, which are handled by attachment processor
    /// </summary>
    IEnumerable<Uri>? GetExtensionUris();

    /// <summary>
    /// Indicates whether attachment processor is supporting incremental processing of attachments
    /// </summary>
    /// <remarks>
    /// `SupportsIncrementalProcessing` should indicate if attachment processor is supporting incremental processing of attachments. It means that `ProcessAttachmentSetsAsync` should be [associative](https://en.wikipedia.org/wiki/Associative_property).
    /// By default `SupportsIncrementalProcessing` should be `False`, unless processing can take longer time and it's beneficial to start the process as soon as possible.
    ///
    /// If `SupportsIncrementalProcessing` is `True` Test Platform may try to speed up whole process by reprocessing data collector attachments as soon as possible when any two test executions are done.For example let's assume we have 5 test executions which are generating 5 data collector attachments: `a1`, `a2`, `a3`, `a4` and `a5`. Test platform could perform invocations:
    /// * `var result1 = await ProcessAttachmentSetsAsync([a1, a2, a3], ...);` when first 3 executions are done
    /// * `var result2 = await ProcessAttachmentSetsAsync(result1.Concat([a4]), ...);` when 4th execution is done
    /// * `var finalResult = await ProcessAttachmentSetsAsync(result2.Concat([a5]), ...);` when last test execution is done
    ///
    /// If `SupportsIncrementalProcessing` is `False` then Test Platform will wait for all test executions to finish and call `ProcessAttachmentSetsAsync` only once:
    /// * `var finalResult = await ProcessAttachmentSetsAsync([a1, a2, a3, a4, a5], ...);`
    /// </remarks>
    bool SupportsIncrementalProcessing { get; }

    /// <summary>
    /// Reprocess attachments generated by independent test executions
    /// </summary>
    /// <param name="configurationElement">Configuration of the attachment processor. Will be the same as the data collector that registers it.</param>
    /// <param name="attachments">Attachments to be processed</param>
    /// <param name="progressReporter">Progress reporter. Accepts integers from 0 to 100</param>
    /// <param name="logger">Message logger</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Attachments after reprocessing</returns>
    Task<ICollection<AttachmentSet>> ProcessAttachmentSetsAsync(XmlElement configurationElement, ICollection<AttachmentSet> attachments, IProgress<int> progressReporter, IMessageLogger logger, CancellationToken cancellationToken);
}
