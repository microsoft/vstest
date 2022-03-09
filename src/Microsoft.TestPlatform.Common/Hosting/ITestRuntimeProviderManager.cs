﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;

namespace Microsoft.VisualStudio.TestPlatform.Common.Hosting;

internal interface ITestRuntimeProviderManager
{
    ITestRuntimeProvider GetTestHostManagerByRunConfiguration(string runConfiguration);
    ITestRuntimeProvider GetTestHostManagerByUri(string hostUri);
}
