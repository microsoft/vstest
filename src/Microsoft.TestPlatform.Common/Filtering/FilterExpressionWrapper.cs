// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Filtering
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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

        private string fastFilterPropertyName;
        private HashSet<string> fastFilter;

        private bool UseFastFilter => fastFilter != null;

        /// <summary>
        /// Initializes FilterExpressionWrapper with given filterString.  
        /// </summary>
        public FilterExpressionWrapper(string filterString)
        {
            ValidateArg.NotNullOrEmpty(filterString, "filterString");
            
            this.FilterString = filterString;
            try
            {
                this.filterExpression = FilterExpression.Parse(filterString, out this.fastFilter, out this.fastFilterPropertyName);
                if (UseFastFilter)
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
            if (this.UseFastFilter)
            {
                // If the property name for fast filter is "NFQN", we will check if FQN is supported.
                var propertyName = fastFilterPropertyName.Equals(FilterExpression.NormalizedFullyQualifiedNameFilterKeyword, StringComparison.OrdinalIgnoreCase) 
                    ? FilterExpression.FullyQualifiedNamePropertyName 
                    : fastFilterPropertyName;

                return supportedProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase) 
                    ? null 
                    : new[] { propertyName };
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
            if (this.UseFastFilter)
            {
                if (fastFilterPropertyName.Equals(FilterExpression.NormalizedFullyQualifiedNameFilterKeyword, StringComparison.OrdinalIgnoreCase))
                {
                    return TryGetSinglePropertyValue(FilterExpression.FullyQualifiedNamePropertyName, propertyValueProvider, out var value)
                            && this.fastFilter.Contains(MakeNormalizedFQN(value));
                }
                else
                {
                    return TryGetSinglePropertyValue(fastFilterPropertyName, propertyValueProvider, out var value)
                            && this.fastFilter.Contains(value);
                }
            }
            else
            {
                return this.filterExpression != null &&this.filterExpression.Evaluate(propertyValueProvider);
            }
        }

        private static string MakeNormalizedFQN(string value)
        {
            var indexOfSpace = value.IndexOf(" ");
            return indexOfSpace > 0 ? value.Substring(0, value.IndexOf(" ")) : value;
        }

        private bool TryGetSinglePropertyValue(string name, Func<string, Object> propertyValueProvider, out string singleValue)
        {
            singleValue = propertyValueProvider(name) as string;
            return singleValue != null;
        }
    }
}
