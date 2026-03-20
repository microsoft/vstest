// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

using Newtonsoft.Json.Serialization;

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization.Legacy;

/// <summary>
/// Original Newtonsoft.Json v1-protocol contract resolver for the legacy fallback serializer.
/// </summary>
internal class LegacyTestPlatformContractResolver1 : LegacyDefaultTestPlatformContractResolver
{
    /// <inheritdoc/>
    protected override JsonContract CreateContract(Type objectType)
    {
        var contract = base.CreateContract(objectType);
        if (typeof(TestCase) == objectType)
        {
            contract.Converter = new LegacyTestCaseConverter();
        }
        else if (typeof(TestResult) == objectType)
        {
            contract.Converter = new LegacyTestResultConverter();
        }

        return contract;
    }
}
