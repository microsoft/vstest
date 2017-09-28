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

        /// <remarks>
        /// Exposed for testing purpose.
        /// </remarks>
        internal readonly FastFilter fastFilter;

        private bool UseFilterExpression => this.filterExpression != null;

        private bool UseFastFilter => this.fastFilter != null;

        /// <summary>
        /// Initializes FilterExpressionWrapper with given filterString and options.  
        /// </summary>
        public FilterExpressionWrapper(string filterString, FilterOptions options = null)
        {
            ValidateArg.NotNullOrEmpty(filterString, "filterString");

            this.FilterString = filterString;
            this.FilterOptions = options;

            try
            {
                // We prefer fast filter when it's available.
                this.filterExpression = FilterExpression.Parse(filterString, out this.fastFilter);

                if (UseFastFilter)
                {
                    this.filterExpression = null;

                    // Property value regex is only supported for fast filter, 
                    // so we ignore it if no fast filter is constructed.

                    // TODO: surface an error message to suer.
                    var regexString = options?.FilterRegEx;
                    if (!string.IsNullOrEmpty(regexString))
                    {
                        this.fastFilter.PropertyValueRegex = new Regex(regexString, RegexOptions.Compiled);
                    }
                }

            }
            catch (FormatException ex)
            {
                this.ParseError = ex.Message;
            }
            catch (ArgumentException)
            { 
                // TODO: surface an error message to user.
                // We don't match any tests in case of a regex parsing error.
                this.fastFilter = null;
            }
        }

        /// <summary>
        /// Initializes FilterExpressionWrapper with given filterString.  
        /// </summary>
        public FilterExpressionWrapper(string filterString)
            : this(filterString, null)
        {
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
        /// User specified additional filter options.
        /// </summary>
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

            if (UseFastFilter)
            {
                invalidProperties = this.fastFilter.ValidForProperties(supportedProperties);
            }
            else if (UseFilterExpression)
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

            return (UseFilterExpression && this.filterExpression.Evaluate(propertyValueProvider)) || 
                   (UseFastFilter && this.fastFilter.Evaluate(propertyValueProvider));
        }
    }
}
