// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceCollector
{
    using System;
    using TestPlatform.ObjectModel.DataCollection;

    internal interface IDataCollectionEvents
    {
        event EventHandler<SessionEndEventArgs> SessionEnd;

        event EventHandler<SessionStartEventArgs> SessionStart;

        event EventHandler<TestCaseEndEventArgs> TestCaseEnd;

        event EventHandler<TestCaseStartEventArgs> TestCaseStart;
    }
}