// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceCollector
{
    using TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// Wrapper for <see cref="Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection.DataCollectionEnvironmentContext"/>
    /// to make the collector testable.
    /// </summary>
    internal interface IDataCollectionAgentContext
    {
    DataCollectionContext SessionDataCollectionContext { get; }
    }
}
