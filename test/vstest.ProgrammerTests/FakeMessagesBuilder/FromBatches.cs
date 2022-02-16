// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FakeMessagesBuilder;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

internal class FromBatches
{
    private List<List<TestResult>> _testResultBatches;

    public FromBatches(List<List<TestResult>> testResultBatches)
    {
        _testResultBatches = testResultBatches;
    }
}