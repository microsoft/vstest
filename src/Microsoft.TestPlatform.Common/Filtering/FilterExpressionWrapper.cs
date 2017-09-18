// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Filtering
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Class holds information related to filtering criteria.
    /// </summary>
    public class FilterExpressionWrapper
    {
        /// <summary>
        /// FilterExpression corresponding to filter criteria
        /// </summary>
        private FilterExpression filterExpression;

        private HashSet<string> filterForRunByFullyQualifiedName;

        private bool IsRunByFullyQualifiedName => filterForRunByFullyQualifiedName != null;

        /// <summary>
        /// Initializes FilterExpressionWrapper with given filterString.  
        /// </summary>
        public FilterExpressionWrapper(string filterString)
        {
            ValidateArg.NotNullOrEmpty(filterString, "filterString");

            this.FilterString = filterString;
            try
            {
                this.filterExpression = FilterExpression.Parse(filterString, out this.filterForRunByFullyQualifiedName);
                if (IsRunByFullyQualifiedName)
                {
                    this.filterExpression = null;
                }
            }
            catch (FormatException ex)
            {
                this.ParseError = ex.Message;
            }
        }

        /// <summary>
        /// User specified filter criteria.
        /// </summary>
        public string FilterString
        {
            get;
            private set;
        }

        /// <summary>
        /// Parsing error (if any), when parsing 'FilterString' with built-in parser.
        /// </summary>
        public string ParseError
        {
            get;
            private set;
        }

        /// <summary>
        /// Validate if underlying filter expression is valid for given set of supported properties.
        /// </summary>
        public string[] ValidForProperties(IEnumerable<String> supportedProperties, Func<string, TestProperty> propertyProvider)
        {
            if (this.IsRunByFullyQualifiedName)
            {
                return supportedProperties.Contains(FilterExpression.FullyQualifiedNamePropertyName, StringComparer.OrdinalIgnoreCase) 
                    ? null 
                    : new[] { FilterExpression.FullyQualifiedNamePropertyName };
            }

            string[] invalidProperties = null;
            if (null != this.filterExpression)
            {
                invalidProperties = this.filterExpression.ValidForProperties(supportedProperties, propertyProvider);
            }
            return invalidProperties;
        }

        /// <summary>
        /// Evaluate filterExpression with given propertyValueProvider.
        /// </summary>
        public bool Evaluate(Func<string, Object> propertyValueProvider)
        {
            ValidateArg.NotNull(propertyValueProvider, "propertyValueProvider");
            if (this.IsRunByFullyQualifiedName)
            {
                if (!TryGetPropertyValue(FilterExpression.FullyQualifiedNamePropertyName, propertyValueProvider, out var value, out var _) || value == null)
                {
                    return false;
                }
                return this.filterForRunByFullyQualifiedName.Contains(value);
            }
            else
            {
                if (null == this.filterExpression)
                {
                    return false;
                }
                return this.filterExpression.Evaluate(propertyValueProvider);
            }
        }

        private bool TryGetPropertyValue(string name, Func<string, Object> propertyValueProvider, out string singleValue, out string[] multiValue)
        {
            var propertyValue = propertyValueProvider(name);
            if (null != propertyValue)
            {
                singleValue = propertyValue as string;
                multiValue = singleValue != null ? null : propertyValue as string[];
                return true;
            }

            singleValue = null;
            multiValue = null;
            return false;
        }
    }
}
