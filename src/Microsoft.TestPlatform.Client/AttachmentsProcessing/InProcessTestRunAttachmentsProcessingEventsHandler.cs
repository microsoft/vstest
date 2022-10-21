// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Microsoft.VisualStudio.TestPlatform.Client;

internal class InProcessTestRunAttachmentsProcessingEventsHandler : ITestRunAttachmentsProcessingEventsHandler
{
    private readonly ITestRunAttachmentsProcessingEventsHandler _oldEventsHandler;

    public InProcessTestRunAttachmentsProcessingEventsHandler(
        ITestRunAttachmentsProcessingEventsHandler oldEventsHandler)
    {
        _oldEventsHandler = oldEventsHandler;
    }

    public void HandleLogMessage(TestMessageLevel level, string? message)
    {
        _oldEventsHandler.HandleLogMessage(level, message);
    }

    public void HandleProcessedAttachmentsChunk(IEnumerable<AttachmentSet> attachments)
    {
        // Not implemented by design, keep in sync with the same named method from
        // TestRunAttachmentsProcessingEventsHandler.cs.
        throw new NotImplementedException();
    }

    public void HandleRawMessage(string rawMessage)
    {
        // No-Op
    }

    public void HandleTestRunAttachmentsProcessingComplete(
        TestRunAttachmentsProcessingCompleteEventArgs attachmentsProcessingCompleteEventArgs,
        IEnumerable<AttachmentSet>? lastChunk)
    {
        _oldEventsHandler.HandleTestRunAttachmentsProcessingComplete(
            attachmentsProcessingCompleteEventArgs,
            lastChunk);
    }

    public void HandleTestRunAttachmentsProcessingProgress(
        TestRunAttachmentsProcessingProgressEventArgs attachmentsProcessingProgressEventArgs)
    {
        _oldEventsHandler.HandleTestRunAttachmentsProcessingProgress(
            attachmentsProcessingProgressEventArgs);
    }
}
