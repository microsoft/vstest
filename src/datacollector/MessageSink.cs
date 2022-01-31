﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector;

using CommunicationUtilities.DataCollection;

using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;

using ObjectModel.Logging;

/// <inheritdoc />
internal class MessageSink : IMessageSink
{
    /// <summary>
    /// Data collection message as sent by DataCollectionLogger.
    /// </summary>
    /// <param name="args">Data collection message event args.</param>
    public void SendMessage(DataCollectionMessageEventArgs args)
    {
        DataCollectionRequestHandler.Instance.SendDataCollectionMessage(args);
    }
}