// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.Common.DataCollector;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces
{
    internal interface IDataCollectionTelemetryManager
    {
        void OnEnvironmentVariableAdded(DataCollectorInformation dataCollectorInformation, string name, string value);
        void OnEnvironmentVariableConflict(DataCollectorInformation dataCollectorInformation, string name, string existingValue);
        IRequestData GetRequestData();
    }
}
