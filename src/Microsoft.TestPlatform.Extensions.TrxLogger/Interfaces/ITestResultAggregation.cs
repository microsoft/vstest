// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel;

using System.Collections.Generic;

internal interface ITestResultAggregation : ITestResult
{
    List<ITestResult> InnerResults { get; }
}
