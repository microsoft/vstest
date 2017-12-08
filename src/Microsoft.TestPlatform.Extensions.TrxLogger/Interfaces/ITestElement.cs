// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    internal interface ITestElement
    {
        TestId Id { get; }
        string Name { get; set; }
        string Owner { get; set; }
        int Priority { get; set; }
        string Storage { get; set; }
        TestExecId ExecutionId { get; set; }
        TestExecId ParentExecutionId { get; set; }
        bool IsRunnable { get; }
        TestListCategoryId CategoryId { get; set; }
        TestCategoryItemCollection TestCategories { get; }
        TestType TestType { get; }
        string Adapter { get; }
    }
}
