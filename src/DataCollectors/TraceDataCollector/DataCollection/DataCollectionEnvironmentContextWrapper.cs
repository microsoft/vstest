// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceCollector
{
    using TestPlatform.ObjectModel.DataCollection;

    internal sealed class DataCollectionEnvironmentContextWrapper : IDataCollectionAgentContext
    {
        private readonly DataCollectionEnvironmentContext environmentContext;

        public DataCollectionEnvironmentContextWrapper(DataCollectionEnvironmentContext environmentContext)
        {
            this.environmentContext = environmentContext;
        }

        public DataCollectionContext SessionDataCollectionContext
        {
            get { return this.environmentContext.SessionDataCollectionContext; }
        }
    }
}