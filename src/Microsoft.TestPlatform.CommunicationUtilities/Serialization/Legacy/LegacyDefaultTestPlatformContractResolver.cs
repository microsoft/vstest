// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

using Newtonsoft.Json.Serialization;

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization.Legacy;

/// <summary>
/// Original Newtonsoft.Json contract resolver for the legacy fallback serializer.
/// </summary>
internal class LegacyDefaultTestPlatformContractResolver : DefaultContractResolver
{
    /// <inheritdoc/>
    protected override JsonContract CreateContract(Type objectType)
    {
        var contract = base.CreateContract(objectType);

        if (typeof(List<KeyValuePair<TestProperty, object>>) == objectType)
        {
            contract.Converter = new LegacyTestObjectConverter();
        }
        else if (objectType == typeof(ITestRunStatistics))
        {
            contract.Converter = new LegacyTestRunStatisticsConverter();
        }

        return contract;
    }
}
