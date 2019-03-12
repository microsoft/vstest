// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceCollector
{
    using System;
    using TestPlatform.ObjectModel.DataCollection;

    /// <inheritdoc />
    internal sealed class DataCollectionEventsWrapper : IDataCollectionEvents
    {
        private readonly DataCollectionEvents wrapped;

        public DataCollectionEventsWrapper(DataCollectionEvents wrapped)
        {
            this.wrapped = wrapped;
        }

        #region IDataCollectionEvents Members

        event EventHandler<SessionEndEventArgs> IDataCollectionEvents.SessionEnd
        {
            add { this.wrapped.SessionEnd += value; }
            remove { this.wrapped.SessionEnd -= value; }
        }

        event EventHandler<SessionStartEventArgs> IDataCollectionEvents.SessionStart
        {
            add { this.wrapped.SessionStart += value; }
            remove { this.wrapped.SessionStart -= value; }
        }

        event EventHandler<TestCaseEndEventArgs> IDataCollectionEvents.TestCaseEnd
        {
            add { this.wrapped.TestCaseEnd += value; }
            remove { this.wrapped.TestCaseEnd -= value; }
        }

        event EventHandler<TestCaseStartEventArgs> IDataCollectionEvents.TestCaseStart
        {
            add { this.wrapped.TestCaseStart += value; }
            remove { this.wrapped.TestCaseStart -= value; }
        }

        #endregion
    }
}