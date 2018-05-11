// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceCollector
{
    using System;
    using TestPlatform.ObjectModel.DataCollection;

    internal sealed class DataCollectionLoggerWrapper : IDataCollectionLogger
    {
        private readonly DataCollectionLogger wrapped;

        public DataCollectionLoggerWrapper(DataCollectionLogger wrapped)
        {
            this.wrapped = wrapped;
        }

        #region IDataCollectionLogger Members

        void IDataCollectionLogger.LogError(DataCollectionContext context, Exception exception)
        {
            this.wrapped.LogError(context, exception);
        }

        void IDataCollectionLogger.LogError(DataCollectionContext context, string text)
        {
            this.wrapped.LogError(context, text);
        }

        void IDataCollectionLogger.LogError(DataCollectionContext context, string text, Exception exception)
        {
            this.wrapped.LogError(context, text, exception);
        }

        void IDataCollectionLogger.LogWarning(DataCollectionContext context, string text)
        {
            this.wrapped.LogWarning(context, text);
        }

        #endregion
    }
}