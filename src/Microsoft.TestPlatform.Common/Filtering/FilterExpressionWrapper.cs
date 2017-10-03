// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Filtering
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using System.Diagnostics;

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

        private bool UseFastFilter => this.fastFilter != null;

        /// <summary>
        /// Initializes FilterExpressionWrapper with given filterString and options.  
        /// </summary>
        public FilterExpressionWrapper(string filterString, FilterOptions options)
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
                        Debug.Assert(options.FilterRegExReplacement != null ? options.FilterRegEx != null : true);
                        this.fastFilter.PropertyValueRegex = new Regex(regexString, RegexOptions.Compiled);
                        this.fastFilter.PropertyValueRegexReplacement = options.FilterRegExReplacement;
                    }
                }

            }
            catch (FormatException ex)
            {
                this.ParseError = ex.Message;
            }
            catch (ArgumentException ex)
            {
                this.fastFilter = null;
                this.ParseError = ex.Message;
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
            Debug.Assert(this.filterExpression != null || this.fastFilter != null);
            return UseFastFilter ? this.fastFilter.ValidForProperties(supportedProperties) : this.filterExpression.ValidForProperties(supportedProperties, propertyProvider);
        }

        /// <summary>
        /// Evaluate filterExpression with given propertyValueProvider.
        /// </summary>
        public bool Evaluate(Func<string, Object> propertyValueProvider)
        {
            ValidateArg.NotNull(propertyValueProvider, "propertyValueProvider");

            Debug.Assert(this.filterExpression != null || this.fastFilter != null);
            return UseFastFilter ? this.fastFilter.Evaluate(propertyValueProvider) : this.filterExpression.Evaluate(propertyValueProvider);
        }
    }
}
