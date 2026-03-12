// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests;

/// <summary>
/// Common "marker" class for compatibility sources, to make finding them easier.
/// </summary>
public abstract class CompatibilityDataSourceAttribute : TestDataSourceAttribute<RunnerInfo>
{
    public CompatibilityDataSourceAttribute()
    {
    }
}
