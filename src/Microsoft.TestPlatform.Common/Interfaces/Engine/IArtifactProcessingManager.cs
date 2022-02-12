// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

using System.Threading.Tasks;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

internal interface IArtifactProcessingManager
{
    void CollectArtifacts(TestRunCompleteEventArgs testRunCompleteEventArgs, string runSettingsXml);
    Task PostProcessArtifactsAsync();
}
