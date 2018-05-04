// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage.Interfaces
{
    using System;
    using System.Xml;
    using TestPlatform.ObjectModel.DataCollection;
    using TraceCollector;
    using IDataCollectionSink = TraceCollector.IDataCollectionSink;

    /// <summary>
    /// The IDynamicCoverageDataCollectorImpl interface.
    /// </summary>
    internal interface IDynamicCoverageDataCollectorImpl : IDisposable
    {
        string GetSessionName();

        void Initialize(XmlElement configurationElement, IDataCollectionSink dataSink, IDataCollectionLogger logger);

        void SessionEnd(object sender, SessionEndEventArgs sessionEndEventArgs);

        void SessionStart(object sender, SessionStartEventArgs sessionStartEventArgs);
    }
}