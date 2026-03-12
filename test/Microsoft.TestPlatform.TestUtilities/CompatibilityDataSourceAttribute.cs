// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests;

/// <summary>
/// Common "marker" class for compatibility sources, to make finding them easier.
/// </summary>
public abstract class CompatibilityDataSourceAttribute : TestDataSourceAttribute<TestDataRow<RunnerInfo>>
{
    public CompatibilityDataSourceAttribute()
    {
    }
}
