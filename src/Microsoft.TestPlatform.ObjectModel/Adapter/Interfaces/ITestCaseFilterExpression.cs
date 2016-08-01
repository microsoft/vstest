// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter
{
    using System;

    /// <summary>
    /// It represents expression for filtering test cases. 
    /// </summary>
    public interface ITestCaseFilterExpression
    {
        /// <summary>
        /// Gets original string for test case filter.
        /// </summary>
        string TestCaseFilterValue { get; }

        /// <summary>
        /// Matched test case with test case filtering criteria.
        /// </summary>
        bool MatchTestCase(TestCase testCase, Func<string, object> propertyValueProvider);
    }
}