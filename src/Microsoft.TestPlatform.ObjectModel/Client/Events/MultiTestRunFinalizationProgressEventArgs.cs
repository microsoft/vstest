// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public class MultiTestRunFinalizationProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="currentHandlerIndex">Specifies current handler index.</param>
        /// <param name="currentHandlerName">Specifies current handler name.</param>
        /// <param name="currentHandlerProgress">Specifies current handler progress.</param>
        /// <param name="handlersCount">Specifies the overall number of handlers.</param>
        public MultiTestRunFinalizationProgressEventArgs(long currentHandlerIndex, string currentHandlerName, long currentHandlerProgress, long handlersCount)
        {
            CurrentHandlerIndex = currentHandlerIndex;
            CurrentHandlerName = currentHandlerName;
            CurrentHandlerProgress = currentHandlerProgress;
            HandlersCount = handlersCount;
        }

        /// <summary>
        /// Gets a current handler index.
        /// </summary>
        [DataMember]
        public long CurrentHandlerIndex { get; private set; }

        /// <summary>
        /// Gets a current handler name.
        /// </summary>
        [DataMember]
        public string CurrentHandlerName { get; private set; }

        /// <summary>
        /// Gets a current handler progress.
        /// </summary>
        [DataMember]
        public long CurrentHandlerProgress { get; private set; }

        /// <summary>
        /// Gets the overall number of handlers.
        /// </summary>
        [DataMember]
        public long HandlersCount { get; private set; }
    }
}
