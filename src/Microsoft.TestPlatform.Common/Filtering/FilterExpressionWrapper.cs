// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Filtering
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <summary>
    /// Class holds information related to filtering criteria.
    /// </summary>
    public class FilterExpressionWrapper
    {
        /// <summary>
        /// FilterExpression corresponding to filter criteria
        /// </summary>
        private readonly FilterExpression filterExpression;

        private readonly Regex filterRegex;

        /// <summary>
        /// Initializes FilterExpressionWrapper with given filterString.  
        /// </summary>
        public FilterExpressionWrapper(string filterString, FilterOptions options = null)
        {
            ValidateArg.NotNullOrEmpty(filterString, "filterString");
            
            this.FilterString = filterString;
            this.FilterOptions = options;            

            try
            {
                this.filterExpression = FilterExpression.Parse(filterString);

                var regexString = options?.FilterRegEx;
                if (!string.IsNullOrEmpty(regexString))
                {
                    filterRegex = new Regex(regexString);
                }
            }
            catch (FormatException ex)
            {
                this.ParseError = ex.Message;
            }
            catch (ArgumentException)
            {
                // Ignore regex parsing error. Filter with no regex matching.
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

        public FilterOptions FilterOptions
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
            return this.filterExpression != null && this.filterExpression.Evaluate(propertyValueProvider, MatchPropertyValueWithRegex);
        }

        public string MatchPropertyValueWithRegex(string value)
        {
            if (this.filterRegex == null)
            {
                return null;
            }

            try
            {
                var match = this.filterRegex.Match(value);
                if (match.Success)
                {
                    return match.Value;
                }
            }
            catch (RegexMatchTimeoutException)
            {
            }

            return null;
        }
    }
}
