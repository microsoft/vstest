// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

#pragma warning disable IDE1006 // Naming Styles
namespace vstest.ProgrammerTests.CommandLine.Fakes;

internal class FakeDataCollectorAttachmentsProcessorsFactory : IDataCollectorAttachmentsProcessorsFactory
{
    public DataCollectorAttachmentProcessor[] Create(InvokedDataCollector[] invokedDataCollectors, IMessageLogger logger)
    {
        throw new NotImplementedException();
    }
}
