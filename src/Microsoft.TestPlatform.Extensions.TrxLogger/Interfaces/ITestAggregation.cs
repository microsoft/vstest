// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Collections.Generic;

    internal interface ITestAggregation : ITestElement
    {
        Dictionary<Guid, TestLink> TestLinks { get; }
    }
}
