﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Hosting;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;

internal interface ITestRuntimeProviderManager
{
    ITestRuntimeProvider GetTestHostManagerByRunConfiguration(string runConfiguration);
    ITestRuntimeProvider GetTestHostManagerByUri(string hostUri);
}
