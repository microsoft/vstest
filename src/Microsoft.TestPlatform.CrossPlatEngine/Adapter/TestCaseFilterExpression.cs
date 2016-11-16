// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

    /// <summary>
    /// Implements ITestCaseFilterExpression, providing test case filtering functionality.
    /// </summary>
    public class TestCaseFilterExpression : ITestCaseFilterExpression
    {
        private FilterExpressionWrapper filterWrapper;

        /// <summary>
        /// If filter Expression is valid for performing TestCase matching 
        /// (has only supported properties, syntax etc)
        /// </summary>
        private bool validForMatch;


        /// <summary>
        /// Adapter specific filter expression.
        /// </summary>
        public TestCaseFilterExpression(FilterExpressionWrapper filterWrapper)
        {
            ValidateArg.NotNull(filterWrapper, "filterWrapper");
            this.filterWrapper = filterWrapper;
            this.validForMatch = string.IsNullOrEmpty(filterWrapper.ParseError);
        }

        /// <summary>
        /// User specified filter criteria.
        /// </summary>
        public string TestCaseFilterValue
        {
            get
            {
                return this.filterWrapper.FilterString;
            }
        }

        /// <summary>
        /// Validate if underlying filter expression is valid for given set of supported properties.
        /// </summary>
        public string[] ValidForProperties(IEnumerable<String> supportedProperties, Func<string, TestProperty> propertyProvider)
        {
            string[] invalidProperties = null;
            if (null != this.filterWrapper && this.validForMatch)
            {
                invalidProperties = this.filterWrapper.ValidForProperties(supportedProperties, propertyProvider);
                if (null != invalidProperties)
                {
                    this.validForMatch = false;
                }
            }
            return invalidProperties;
        }

        /// <summary>
        /// Match test case with filter criteria.
        /// </summary>
        public bool MatchTestCase(TestCase testCase, Func<string, Object> propertyValueProvider)
        {
            ValidateArg.NotNull(testCase, "testCase");
            ValidateArg.NotNull(propertyValueProvider, "propertyValueProvider");
            if (!this.validForMatch)
            {
                return false;
            }

            if (null == this.filterWrapper)
            {
                // can be null when parsing error occurs. Invalid filter results in no match.
                return false;
            }
            return this.filterWrapper.Evaluate(propertyValueProvider);
        }

    }
}
